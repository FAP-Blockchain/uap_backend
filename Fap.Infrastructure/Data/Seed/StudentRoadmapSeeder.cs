using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds StudentRoadmaps - academic progress tracking for students
    /// </summary>
    public class StudentRoadmapSeeder : BaseSeeder
    {
        public StudentRoadmapSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.StudentRoadmaps.AnyAsync())
            {
                Console.WriteLine("⏭️  Student Roadmaps already exist. Skipping...");
                return;
            }

            var roadmaps = new List<StudentRoadmap>();

            // Get all students with their curriculum
            var students = await _context.Students
                .Include(s => s.Curriculum)
                .ToListAsync();

            // Get all semesters
            var semesters = await _context.Semesters.OrderBy(s => s.StartDate).ToListAsync();

            if (!students.Any() || !semesters.Any())
            {
                Console.WriteLine("⚠️  Missing required data for roadmaps. Skipping...");
                return;
            }

            // Pre-fetch curriculum subjects for all curriculums
            var curriculumSubjects = await _context.CurriculumSubjects
                .Include(cs => cs.Subject)
                .ToListAsync();

            var random = new Random(77777);

            Console.WriteLine("   🧪 Creating student roadmaps based on curriculum...");

            foreach (var student in students)
            {
                if (student.CurriculumId == 0) continue;

                // Get subjects for this student's curriculum
                var studentSubjects = curriculumSubjects
                    .Where(cs => cs.CurriculumId == student.CurriculumId)
                    .OrderBy(cs => cs.SemesterNumber)
                    .Select(cs => new { cs.Subject, cs.SemesterNumber })
                    .ToList();

                if (!studentSubjects.Any()) continue;

                // Apply different scenarios based on student index or ID
                // We can use a simple modulo or check specific IDs
                
                if (student.Id == TeacherStudentSeeder.Student1Id)
                {
                    // Scenario 1: Perfect student (SE)
                    CreatePerfectStudentRoadmap(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student2Id)
                {
                    // Scenario 2: Missing prerequisites (SE)
                    CreateStudentMissingPrerequisites(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student3Id)
                {
                    // Scenario 3: Completed some (SE)
                    CreateStudentWithCompletedSubjects(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student4Id)
                {
                    // Scenario 4: In Progress (SE)
                    CreateStudentInProgress(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student5Id) // IA Student
                {
                    // Scenario 5: IA Student - Good progress
                    CreateStudentWithCompletedSubjects(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student6Id) // IA Student
                {
                    // Scenario 6: IA Student - Fresh/In Progress
                    CreateStudentInProgress(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student7Id) // GD Student
                {
                    // Scenario 7: GD Student - Perfect
                    CreatePerfectStudentRoadmap(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
                else if (student.Id == TeacherStudentSeeder.Student8Id) // GD Student
                {
                    // Scenario 8: GD Student - Fresh
                    CreateFreshStudent(student, studentSubjects.Select(s => s.Subject).ToList(), semesters, roadmaps);
                }
            }

            await _context.StudentRoadmaps.AddRangeAsync(roadmaps);
            await SaveAsync("Student Roadmaps");

            Console.WriteLine($"   ✅ Created {roadmaps.Count} student roadmap entries:");
            Console.WriteLine($"      • Completed: {roadmaps.Count(r => r.Status == "Completed")}");
            Console.WriteLine($"      • In Progress: {roadmaps.Count(r => r.Status == "InProgress")}");
            Console.WriteLine($"      • Planned: {roadmaps.Count(r => r.Status == "Planned")}");
            Console.WriteLine($"      • Failed: {roadmaps.Count(r => r.Status == "Failed")}");
        }

        // ==================== TEST SCENARIO CREATORS ====================

        private void CreatePerfectStudentRoadmap(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            int sequenceOrder = 1;
            foreach (var subject in subjects.Take(10)) // First 10 subjects
            {
                var semesterIndex = (sequenceOrder - 1) / 3;
                if (semesterIndex >= semesters.Count) semesterIndex = semesters.Count - 1;
                var semester = semesters[semesterIndex];

                // Past semesters: Completed
                // Current semester: InProgress
                // Future semesters: Planned
                string status = semester.EndDate < DateTime.UtcNow ? "Completed"
                     : semester.StartDate <= DateTime.UtcNow && semester.EndDate >= DateTime.UtcNow ? "InProgress"
                     : "Planned";

                roadmaps.Add(CreateRoadmapEntry(
                    student.Id, subject.Id, semester.Id, sequenceOrder, status, 8.5m, subject.SubjectName));
                sequenceOrder++;
            }
        }

        private void CreateStudentMissingPrerequisites(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            // Only completed 2 subjects, has many planned (missing prerequisites)
            if (subjects.Count >= 5)
            {
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[0].Id, semesters[0].Id, 1, "Completed", 7.0m, subjects[0].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[1].Id, semesters[0].Id, 2, "Completed", 6.5m, subjects[1].SubjectName));

                // Advanced subjects planned but prerequisites not met
                for (int i = 2; i < Math.Min(subjects.Count, 8); i++)
                {
                    var semesterIndex = i / 3;
                    if (semesterIndex >= semesters.Count) semesterIndex = semesters.Count - 1;
                    roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[i].Id, semesters[semesterIndex].Id, i + 1, "Planned", null, subjects[i].SubjectName));
                }
            }
        }

        private void CreateStudentWithCompletedSubjects(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            // Completed 5 subjects, planning next 5
            for (int i = 0; i < Math.Min(subjects.Count, 10); i++)
            {
                var semesterIndex = i / 3;
                if (semesterIndex >= semesters.Count) semesterIndex = semesters.Count - 1;
                var semester = semesters[semesterIndex];
                var status = i < 5 ? "Completed" : "Planned";
                var score = i < 5 ? 7.5m + (i * 0.3m) : (decimal?)null;

                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[i].Id, semester.Id, i + 1, status, score, subjects[i].SubjectName));
            }
        }

        private void CreateStudentInProgress(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            // Completed 3, InProgress 3, Planned rest
            var currentSemester = semesters.FirstOrDefault(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                ?? semesters.Last();

            for (int i = 0; i < Math.Min(subjects.Count, 9); i++)
    {
       var status = i < 3 ? "Completed" : i < 6 ? "InProgress" : "Planned";
     var semesterIndex = i / 3;
    if (semesterIndex >= semesters.Count) semesterIndex = semesters.Count - 1;
   var semester = i < 6 ? currentSemester : semesters[semesterIndex];
          var score = i < 3 ? 8.0m : (decimal?)null;

       roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[i].Id, semester.Id, i + 1, status, score, subjects[i].SubjectName));
}
        }

        private void CreateStudentWithFailures(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            // Some completed, some failed (need retake)
            if (subjects.Count >= 6)
            {
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[0].Id, semesters[0].Id, 1, "Completed", 8.0m, subjects[0].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[1].Id, semesters[0].Id, 2, "Failed", 3.5m, subjects[1].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[2].Id, semesters[0].Id, 3, "Failed", 4.0m, subjects[2].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[3].Id, semesters[1].Id, 4, "Completed", 7.0m, subjects[3].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[4].Id, semesters[1].Id, 5, "Planned", null, subjects[4].SubjectName));
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[5].Id, semesters[1].Id, 6, "Planned", null, subjects[5].SubjectName));
            }
        }

        private void CreateFreshStudent(
            Student student,
            List<Subject> subjects,
            List<Semester> semesters,
            List<StudentRoadmap> roadmaps)
        {
            // All planned, no completion yet
            for (int i = 0; i < Math.Min(subjects.Count, 12); i++)
            {
                var semesterIndex = i / 3;
                if (semesterIndex >= semesters.Count) semesterIndex = semesters.Count - 1;
                roadmaps.Add(CreateRoadmapEntry(student.Id, subjects[i].Id, semesters[semesterIndex].Id, i + 1, "Planned", null, subjects[i].SubjectName));
            }
        }

        private StudentRoadmap CreateRoadmapEntry(
            Guid studentId,
            Guid subjectId,
            Guid semesterId,
            int sequenceOrder,
            string status,
            decimal? score,
            string subjectName)
        {
            var roadmap = new StudentRoadmap
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                SequenceOrder = sequenceOrder,
                Status = status,
                FinalScore = score,
                LetterGrade = score.HasValue ? ConvertToLetterGrade(score.Value) : string.Empty, // ? FIX: Empty string instead of null
                StartedAt = status != "Planned" ? DateTime.UtcNow.AddDays(-30) : null,
                CompletedAt = status == "Completed" ? DateTime.UtcNow.AddDays(-7) : null,
                Notes = GetRoadmapNotes(status, subjectName) ?? string.Empty,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            return roadmap;
        }

        private string GetRoadmapStatus(Semester semester, Random random)
        {
            var now = DateTime.UtcNow;

            // If semester hasn't started yet
            if (semester.StartDate > now)
            {
                return "Planned";
            }
            // If semester has ended
            else if (semester.EndDate < now)
            {
                // 95% completed, 5% failed
                return random.Next(100) < 95 ? "Completed" : "Failed";
            }
            // If semester is ongoing
            else
            {
                return "InProgress";
            }
        }

        private decimal GenerateFinalScore(Random random)
        {
            var roll = random.Next(100);

            // Score distribution
            if (roll < 20) // 20% excellent (8.5-10)
            {
                return Math.Round((decimal)(8.5 + random.NextDouble() * 1.5), 2);
            }
            else if (roll < 50) // 30% good (7-8.5)
            {
                return Math.Round((decimal)(7.0 + random.NextDouble() * 1.5), 2);
            }
            else if (roll < 80) // 30% average (5.5-7)
            {
                return Math.Round((decimal)(5.5 + random.NextDouble() * 1.5), 2);
            }
            else // 20% below average (3-5.5)
            {
                return Math.Round((decimal)(3.0 + random.NextDouble() * 2.5), 2);
            }
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

        private string? GetRoadmapNotes(string status, string subjectName)
        {
            return status switch
            {
                "Completed" => $"Successfully completed {subjectName}",
                "InProgress" => $"Currently enrolled in {subjectName}",
                "Failed" => "Need to retake this course",
                "Planned" => $"Planning to take {subjectName} next semester",
                _ => null
            };
        }
    }
}
