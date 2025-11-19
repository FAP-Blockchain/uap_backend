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
                Console.WriteLine("??  Student Roadmaps already exist. Skipping...");
                return;
            }

            var roadmaps = new List<StudentRoadmap>();

            // Get all students
            var students = await _context.Students.Take(6).ToListAsync();

            // Get all subjects
            var subjects = await _context.Subjects.OrderBy(s => s.SubjectCode).ToListAsync();

            // Get all semesters
            var semesters = await _context.Semesters.OrderBy(s => s.StartDate).ToListAsync();

            if (!students.Any() || !subjects.Any() || !semesters.Any())
            {
                Console.WriteLine("??  Missing required data for roadmaps. Skipping...");
                return;
            }

            var random = new Random(77777);

            // ? CREATE SPECIFIC TEST SCENARIOS
            Console.WriteLine("   ?? Creating test scenarios...");

            // SCENARIO 1: Student 1 - Perfect student (all prerequisites met, ready for advanced classes)
            if (students.Count > 0)
            {
                CreatePerfectStudentRoadmap(students[0], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 1: Perfect student with all prerequisites");
            }

            // SCENARIO 2: Student 2 - Missing prerequisites (cannot enroll in advanced subjects)
            if (students.Count > 1)
            {
                CreateStudentMissingPrerequisites(students[1], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 2: Student missing prerequisites");
            }

            // SCENARIO 3: Student 3 - Already completed some subjects (test duplicate prevention)
            if (students.Count > 2)
            {
                CreateStudentWithCompletedSubjects(students[2], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 3: Student with completed subjects");
            }

            // SCENARIO 4: Student 4 - Currently in progress (active enrollment)
            if (students.Count > 3)
            {
                CreateStudentInProgress(students[3], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 4: Student with in-progress subjects");
            }

            // SCENARIO 5: Student 5 - Failed some subjects (need retake)
            if (students.Count > 4)
            {
                CreateStudentWithFailures(students[4], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 5: Student with failed subjects");
            }

            // SCENARIO 6: Student 6 - Fresh student (all planned, no completion)
            if (students.Count > 5)
            {
                CreateFreshStudent(students[5], subjects, semesters, roadmaps);
                Console.WriteLine("   ? Scenario 6: Fresh student with all planned");
            }

            await _context.StudentRoadmaps.AddRangeAsync(roadmaps);
            await SaveAsync("Student Roadmaps");

            Console.WriteLine($"   ?? Created {roadmaps.Count} student roadmap entries:");
            Console.WriteLine($"      • Completed: {roadmaps.Count(r => r.Status == "Completed")}");
            Console.WriteLine($"  • In Progress: {roadmaps.Count(r => r.Status == "InProgress")}");
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
                LetterGrade = score.HasValue ? ConvertToLetterGrade(score.Value) : null,
                StartedAt = status != "Planned" ? DateTime.UtcNow.AddDays(-30) : null,
                CompletedAt = status == "Completed" ? DateTime.UtcNow.AddDays(-7) : null,
                Notes = GetRoadmapNotes(status, subjectName),
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
