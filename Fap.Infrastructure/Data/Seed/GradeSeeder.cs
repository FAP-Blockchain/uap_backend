using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Grades with varied test scenarios for different students and subjects
    /// </summary>
    public class GradeSeeder : BaseSeeder
    {
        public GradeSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Grades.AnyAsync())
            {
                Console.WriteLine("Grades already exist. Skipping seeding...");
                return;
            }

            var grades = new List<Grade>();

            // Get all enrollments
            var enrolls = await _context.Enrolls
   .Include(e => e.Class)
           .ThenInclude(c => c.SubjectOffering)
                .ToListAsync();

            // Get all grade components
            var allComponents = await _context.GradeComponents.ToListAsync();
            
            // Identify leaf components (those that are not parents)
            var parentIds = allComponents.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value).Distinct().ToHashSet();
            var leafComponents = allComponents.Where(c => !parentIds.Contains(c.Id)).ToList();

            var random = new Random(54321); // Fixed seed for consistency

            foreach (var enroll in enrolls)
            {
                var subjectId = enroll.Class.SubjectOffering.SubjectId;
                var studentId = enroll.StudentId;

                // Determine student performance level
                var performanceLevel = GetStudentPerformanceLevel(random);
                var isHonorStudent = studentId == TeacherStudentSeeder.Student1Id;

                // Get components for this subject only
                var subjectComponents = leafComponents.Where(c => c.SubjectId == subjectId).ToList();

                // Create grades for each component
                foreach (var component in subjectComponents)
                {
                    // Honor student (Student1) always has completed grades
                    if (!isHonorStudent && random.Next(100) >= 85)
                    {
                        continue; // 85% chance component is graded for others
                    }

                    var score = isHonorStudent
                        ? GenerateHonorScore(component.Name, random)
                        : GenerateScore(performanceLevel, component.Name, random);

                    var grade = new Grade
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentId,
                        SubjectId = subjectId,
                        GradeComponentId = component.Id,
                        Score = score,
                        LetterGrade = ConvertToLetterGrade(score),
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    };

                    grades.Add(grade);
                }
            }

            await _context.Grades.AddRangeAsync(grades);
            await SaveAsync("Grades");

            Console.WriteLine($"   Created {grades.Count} grade records:");
            Console.WriteLine($"      • A/A+ grades: {grades.Count(g => g.LetterGrade == "A" || g.LetterGrade == "A+")}");
            Console.WriteLine($"      • B grades: {grades.Count(g => g.LetterGrade?.StartsWith("B") == true)}");
            Console.WriteLine($"      • C grades: {grades.Count(g => g.LetterGrade?.StartsWith("C") == true)}");
            Console.WriteLine($"      • D/F grades: {grades.Count(g => g.LetterGrade == "D" || g.LetterGrade == "F")}");
        }

        private string GetStudentPerformanceLevel(Random random)
        {
            var roll = random.Next(100);

            if (roll < 20) return "Excellent";      // 20%
            else if (roll < 55) return "Good";       // 35%
            else if (roll < 85) return "Average";    // 30%
            else return "NeedsSupport";              // 15% - still passing
        }

        private decimal GenerateScore(string performanceLevel, string componentName, Random random)
        {
            // Base score range based on performance level
            var (minScore, maxScore) = performanceLevel switch
            {
                "Excellent" => (8.5m, 10.0m),
                "Good" => (7.0m, 8.8m),
                "Average" => (6.0m, 7.5m),
                "NeedsSupport" => (5.0m, 6.2m),
                _ => (6.0m, 7.5m)
            };

            // Adjust for component difficulty
            var adjustment = componentName switch
            {
                "Final Exam" => -0.5m,      // Finals are harder
                "Midterm Exam" => -0.3m,    // Midterms are moderately hard
                "Quiz" => 0.2m,        // Quizzes are easier
                "Attendance & Participation" => 0.5m, // Usually high scores
                _ => 0.0m
            };

            var range = maxScore - minScore;
            var randomValue = (decimal)random.NextDouble() * range;
            var score = minScore + randomValue + adjustment;

            // Clamp between 5 (pass) and 10
            score = Math.Max(5.0m, Math.Min(10m, score));

            // Round to 1 decimal place
            return Math.Round(score, 1);
        }

        private decimal GenerateHonorScore(string componentName, Random random)
        {
            var baseScore = 8.8m + (decimal)random.NextDouble() * 1.2m;
            var adjustment = componentName switch
            {
                "Final Exam" => 0.1m,
                "Midterm Exam" => 0.2m,
                "Quiz" => 0.3m,
                "Attendance & Participation" => 0.5m,
                _ => 0.0m
            };

            var score = Math.Min(10m, baseScore + adjustment);
            return Math.Round(score, 1);
        }

        private string ConvertToLetterGrade(decimal score)
        {
            if (score >= 9.0m) return "A+";
            else if (score >= 8.5m) return "A";
            else if (score >= 8.0m) return "B+";
            else if (score >= 7.0m) return "B";
            else if (score >= 6.5m) return "C+";
            else if (score >= 5.5m) return "C";
            else if (score >= 5.0m) return "D+";
            else if (score >= 4.0m) return "D";
            else return "F";
        }
    }
}
