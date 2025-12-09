using System;
using System.Collections.Generic;
using System.Linq;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class EnrollmentSeeder : BaseSeeder
    {
        public EnrollmentSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.ClassMembers.AnyAsync() || await _context.Enrolls.AnyAsync())
            {
                Console.WriteLine("Enrollments already exist. Skipping seeding...");
                return;
            }

            await SeedClassMembersAsync();
            await SeedEnrollsAsync();
        }

        private async Task SeedClassMembersAsync()
        {
            var assignments = new List<(Guid ClassId, Guid StudentId, DateTime JoinedAt)>
            {
                // Student 1 – Completed SE curriculum (historical record)
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student1Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student1Id, new DateTime(2025, 11, 12)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 1, 15)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 1, 16)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 5, 20)),
                (ClassSeeder.CSD201_Summer2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 5, 21)),
                (ClassSeeder.PRJ301_Fall2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 8, 9)),
                (ClassSeeder.SWP391_Winter2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 9, 5)),
                (ClassSeeder.SWT301_Winter2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 9, 6)),
                (ClassSeeder.SEP490_Spring2027_A, TeacherStudentSeeder.Student1Id, new DateTime(2027, 2, 1)),

                // Student 2 – Early SE student (stops after introductory courses)
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student2Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student2Id, new DateTime(2025, 11, 12)),

                // Student 3 – Completed five SE subjects
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student3Id, new DateTime(2025, 11, 11)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student3Id, new DateTime(2025, 11, 13)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2026, 1, 12)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2026, 1, 16)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2026, 5, 20)),

                // Student 4 – SE student currently in semester 3
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student4Id, new DateTime(2025, 11, 12)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student4Id, new DateTime(2025, 11, 13)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 1, 17)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 1, 18)),
                (ClassSeeder.CSD201_Summer2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 5, 21)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 5, 22)),

                // Student 5 – IA student needing support
                (ClassSeeder.CSI106_Winter2025_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 11, 11)),
                (ClassSeeder.MAE101_Winter2025_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 11, 14)),
                (ClassSeeder.PFP191_Spring2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 1, 17)),
                (ClassSeeder.MAS291_Spring2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 1, 18)),
                (ClassSeeder.NWC204_Summer2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 5, 22)),
                (ClassSeeder.OSG202_Fall2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 8, 10)),
                (ClassSeeder.CRY303c_Fall2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 8, 11)),

                // Student 6 – IA student progressing on schedule
                (ClassSeeder.CSI106_Winter2025_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 11, 12)),
                (ClassSeeder.MAE101_Winter2025_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 11, 14)),
                (ClassSeeder.PFP191_Spring2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 1, 17)),
                (ClassSeeder.MAS291_Spring2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 1, 18)),
                (ClassSeeder.NWC204_Summer2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 5, 23)),
                (ClassSeeder.OSG202_Fall2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 8, 10)),

                // GD students already aligned to their curriculum
                (ClassSeeder.DRP101_Winter2025_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.DTG102_Winter2025_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.DRP101_Winter2025_A, TeacherStudentSeeder.Student8Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.DTG102_Winter2025_A, TeacherStudentSeeder.Student8Id, new DateTime(2025, 11, 10)),
                (ClassSeeder.DRS102_Spring2026_A, TeacherStudentSeeder.Student7Id, new DateTime(2026, 1, 15)),
                (ClassSeeder.VCM202_Spring2026_A, TeacherStudentSeeder.Student7Id, new DateTime(2026, 1, 15))
            };

            var classMembers = assignments.Select(a => new ClassMember
            {
                ClassId = a.ClassId,
                StudentId = a.StudentId,
                JoinedAt = a.JoinedAt
            }).ToList();

            await _context.ClassMembers.AddRangeAsync(classMembers);
            await SaveAsync("ClassMembers");

            Console.WriteLine($"Created {classMembers.Count} class membership records");
        }

        private async Task SeedEnrollsAsync()
        {
            var enrollmentDefinitions = new List<(Guid ClassId, Guid StudentId, DateTime RegisteredAt)>
            {
                // Student 1 – SE track only
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student1Id, new DateTime(2025, 10, 20)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student1Id, new DateTime(2025, 10, 22)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2025, 12, 15)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2025, 12, 16)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 4, 10)),
                (ClassSeeder.CSD201_Summer2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 4, 12)),
                (ClassSeeder.PRJ301_Fall2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 7, 15)),
                (ClassSeeder.SWP391_Winter2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 9, 1)),
                (ClassSeeder.SWT301_Winter2026_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 9, 2)),
                (ClassSeeder.SEP490_Spring2027_A, TeacherStudentSeeder.Student1Id, new DateTime(2026, 12, 15)),

                // Student 2 – Introductory SE
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student2Id, new DateTime(2025, 10, 21)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student2Id, new DateTime(2025, 10, 25)),

                // Student 3 – Five SE completions
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student3Id, new DateTime(2025, 10, 21)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student3Id, new DateTime(2025, 10, 23)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2025, 12, 14)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2025, 12, 18)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student3Id, new DateTime(2026, 4, 10)),

                // Student 4 – Active SE student
                (ClassSeeder.PRF192_Winter2025_A, TeacherStudentSeeder.Student4Id, new DateTime(2025, 10, 22)),
                (ClassSeeder.CEA201_Winter2025_Evening, TeacherStudentSeeder.Student4Id, new DateTime(2025, 10, 24)),
                (ClassSeeder.PRO192_Spring2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2025, 12, 17)),
                (ClassSeeder.MAD101_Spring2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2025, 12, 19)),
                (ClassSeeder.CSD201_Summer2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 4, 11)),
                (ClassSeeder.DBI202_Summer2026_A, TeacherStudentSeeder.Student4Id, new DateTime(2026, 4, 12)),

                // Student 5 – IA support track
                (ClassSeeder.CSI106_Winter2025_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 10, 21)),
                (ClassSeeder.MAE101_Winter2025_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 10, 22)),
                (ClassSeeder.PFP191_Spring2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 12, 20)),
                (ClassSeeder.MAS291_Spring2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2025, 12, 21)),
                (ClassSeeder.NWC204_Summer2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 4, 15)),
                (ClassSeeder.OSG202_Fall2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 7, 15)),
                (ClassSeeder.CRY303c_Fall2026_A, TeacherStudentSeeder.Student5Id, new DateTime(2026, 7, 16)),

                // Student 6 – IA on track
                (ClassSeeder.CSI106_Winter2025_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 10, 22)),
                (ClassSeeder.MAE101_Winter2025_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 10, 23)),
                (ClassSeeder.PFP191_Spring2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 12, 22)),
                (ClassSeeder.MAS291_Spring2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2025, 12, 22)),
                (ClassSeeder.NWC204_Summer2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 4, 16)),
                (ClassSeeder.OSG202_Fall2026_A, TeacherStudentSeeder.Student6Id, new DateTime(2026, 7, 15)),

                // GD Students
                (ClassSeeder.DRP101_Winter2025_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 10, 20)),
                (ClassSeeder.DTG102_Winter2025_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 10, 20)),
                (ClassSeeder.DRP101_Winter2025_A, TeacherStudentSeeder.Student8Id, new DateTime(2025, 10, 20)),
                (ClassSeeder.DTG102_Winter2025_A, TeacherStudentSeeder.Student8Id, new DateTime(2025, 10, 20)),
                (ClassSeeder.DRS102_Spring2026_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 12, 15)),
                (ClassSeeder.VCM202_Spring2026_A, TeacherStudentSeeder.Student7Id, new DateTime(2025, 12, 15))
            };

            var enrolls = enrollmentDefinitions.Select(def => new Enroll
            {
                Id = Guid.NewGuid(),
                ClassId = def.ClassId,
                StudentId = def.StudentId,
                RegisteredAt = def.RegisteredAt,
                IsApproved = true
            }).ToList();

            await _context.Enrolls.AddRangeAsync(enrolls);
            await SaveAsync("Enrolls");

            Console.WriteLine($"Created {enrolls.Count} enrollment records");
        }
    }
}
