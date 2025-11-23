using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Subjects (master data) and SubjectOfferings (semester-specific)
    /// SubjectOffering pattern allows subjects to be offered in multiple semesters
    /// </summary>
    public class SubjectOfferingSeeder : BaseSeeder
    {
        // Subject IDs (Master Data)
        public static readonly Guid SE101Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid SE102Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
        public static readonly Guid DB201Id = Guid.Parse("10000000-0000-0000-0000-000000000003");
        public static readonly Guid WEB301Id = Guid.Parse("10000000-0000-0000-0000-000000000004");
        public static readonly Guid MATH101Id = Guid.Parse("10000000-0000-0000-0000-000000000005");
        public static readonly Guid MATH201Id = Guid.Parse("10000000-0000-0000-0000-000000000006");
        public static readonly Guid CS101Id = Guid.Parse("10000000-0000-0000-0000-000000000007");
        public static readonly Guid CS201Id = Guid.Parse("10000000-0000-0000-0000-000000000008");

        // SubjectOffering IDs (Semester-specific offerings)
        public static readonly Guid SE101_Spring2024 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        public static readonly Guid SE101_Fall2024 = Guid.Parse("20000000-0000-0000-0000-000000000002");
        public static readonly Guid SE102_Spring2024 = Guid.Parse("20000000-0000-0000-0000-000000000003");
        public static readonly Guid SE102_Summer2024 = Guid.Parse("20000000-0000-0000-0000-000000000004");
        public static readonly Guid DB201_Spring2024 = Guid.Parse("20000000-0000-0000-0000-000000000005");
        public static readonly Guid DB201_Fall2024 = Guid.Parse("20000000-0000-0000-0000-000000000006");
        public static readonly Guid WEB301_Summer2024 = Guid.Parse("20000000-0000-0000-0000-000000000007");
        public static readonly Guid WEB301_Fall2024 = Guid.Parse("20000000-0000-0000-0000-000000000008");
        public static readonly Guid MATH101_Spring2024 = Guid.Parse("20000000-0000-0000-0000-000000000009");
        public static readonly Guid MATH201_Fall2024 = Guid.Parse("20000000-0000-0000-0000-00000000000a");
        public static readonly Guid CS101_Spring2024 = Guid.Parse("20000000-0000-0000-0000-00000000000b");
        public static readonly Guid CS201_Summer2024 = Guid.Parse("20000000-0000-0000-0000-00000000000c");

        public SubjectOfferingSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Subjects.AnyAsync())
            {
                Console.WriteLine("Subjects already exist. Skipping seeding...");
                return;
            }

            await SeedSubjectsAsync();
            await SeedSubjectOfferingsAsync();
        }

        private async Task SeedSubjectsAsync()
        {
            var subjects = new List<Subject>
            {
                // ===== SOFTWARE ENGINEERING =====
                new Subject
                {
                    Id = SE101Id,
                    SubjectCode = "SE101",
                    SubjectName = "Introduction to Software Engineering",
                    Description = "Fundamentals of software development lifecycle, methodologies, and best practices",
                    Credits = 3,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = SE102Id,
                    SubjectCode = "SE102",
                    SubjectName = "Advanced Software Engineering",
                    Description = "Design patterns, architecture, testing strategies, and agile methodologies",
                    Credits = 4,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = "SE101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== DATABASE =====
                new Subject
                {
                    Id = DB201Id,
                    SubjectCode = "DB201",
                    SubjectName = "Database Design and Implementation",
                    Description = "Relational database design, SQL, normalization, and database management",
                    Credits = 4,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = "CS101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== WEB DEVELOPMENT =====
                new Subject
                {
                    Id = WEB301Id,
                    SubjectCode = "WEB301",
                    SubjectName = "Modern Web Development",
                    Description = "Full-stack web development with React, Node.js, and RESTful APIs",
                    Credits = 4,
                    Category = "Elective",
                    Department = "Software Engineering",
                    Prerequisites = "SE101,DB201",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== MATHEMATICS =====
                new Subject
                {
                    Id = MATH101Id,
                    SubjectCode = "MATH101",
                    SubjectName = "Calculus I",
                    Description = "Differential and integral calculus, limits, derivatives, and applications",
                    Credits = 4,
                    Category = "General",
                    Department = "Mathematics",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = MATH201Id,
                    SubjectCode = "MATH201",
                    SubjectName = "Discrete Mathematics",
                    Description = "Logic, sets, relations, graph theory, and combinatorics for computer science",
                    Credits = 3,
                    Category = "Core",
                    Department = "Mathematics",
                    Prerequisites = "MATH101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== COMPUTER SCIENCE =====
                new Subject
                {
                    Id = CS101Id,
                    SubjectCode = "CS101",
                    SubjectName = "Programming Fundamentals",
                    Description = "Introduction to programming with C#, algorithms, and problem solving",
                    Credits = 4,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = CS201Id,
                    SubjectCode = "CS201",
                    SubjectName = "Data Structures and Algorithms",
                    Description = "Arrays, linked lists, trees, graphs, sorting, searching, and complexity analysis",
                    Credits = 4,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = "CS101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Subjects.AddRangeAsync(subjects);
            await SaveAsync("Subjects (Master Data)");
        }

        private async Task SeedSubjectOfferingsAsync()
        {
            var offerings = new List<SubjectOffering>
            {
                // ===== SE101: Spring 2024 & Fall 2024 =====
                new SubjectOffering
                {
                    Id = SE101_Spring2024,
                    SubjectId = SE101Id,
                    SemesterId = SemesterSeeder.Spring2024Id,
                    MaxClasses = 5,
                    SemesterCapacity = 200,
                    RegistrationStartDate = new DateTime(2023, 12, 1),
                    RegistrationEndDate = new DateTime(2023, 12, 31),
                    IsActive = true,
                    Notes = "Popular course - high demand expected",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SubjectOffering
                {
                    Id = SE101_Fall2024,
                    SubjectId = SE101Id,
                    SemesterId = SemesterSeeder.Fall2024Id,
                    MaxClasses = 8,
                    SemesterCapacity = 320,
                    RegistrationStartDate = new DateTime(2024, 7, 1),
                    RegistrationEndDate = new DateTime(2024, 8, 15),
                    IsActive = true,
                    Notes = "Expanded capacity for fall semester",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SE102: Spring 2024 & Summer 2024 =====
                new SubjectOffering
                {
                    Id = SE102_Spring2024,
                    SubjectId = SE102Id,
                    SemesterId = SemesterSeeder.Spring2024Id,
                    MaxClasses = 3,
                    SemesterCapacity = 120,
                    RegistrationStartDate = new DateTime(2023, 12, 1),
                    RegistrationEndDate = new DateTime(2023, 12, 31),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SubjectOffering
                {
                    Id = SE102_Summer2024,
                    SubjectId = SE102Id,
                    SemesterId = SemesterSeeder.Summer2024Id,
                    MaxClasses = 2,
                    SemesterCapacity = 80,
                    RegistrationStartDate = new DateTime(2024, 5, 1),
                    RegistrationEndDate = new DateTime(2024, 5, 31),
                    IsActive = true,
                    Notes = "Intensive summer session",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== DB201: Spring 2024 & Fall 2024 =====
                new SubjectOffering
                {
                    Id = DB201_Spring2024,
                    SubjectId = DB201Id,
                    SemesterId = SemesterSeeder.Spring2024Id,
                    MaxClasses = 4,
                    SemesterCapacity = 160,
                    RegistrationStartDate = new DateTime(2023, 12, 1),
                    RegistrationEndDate = new DateTime(2023, 12, 31),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SubjectOffering
                {
                    Id = DB201_Fall2024,
                    SubjectId = DB201Id,
                    SemesterId = SemesterSeeder.Fall2024Id,
                    MaxClasses = 6,
                    SemesterCapacity = 240,
                    RegistrationStartDate = new DateTime(2024, 7, 1),
                    RegistrationEndDate = new DateTime(2024, 8, 15),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== WEB301: Summer 2024 & Fall 2024 =====
                new SubjectOffering
                {
                    Id = WEB301_Summer2024,
                    SubjectId = WEB301Id,
                    SemesterId = SemesterSeeder.Summer2024Id,
                    MaxClasses = 3,
                    SemesterCapacity = 90,
                    RegistrationStartDate = new DateTime(2024, 5, 1),
                    RegistrationEndDate = new DateTime(2024, 5, 31),
                    IsActive = true,
                    Notes = "Hands-on web development bootcamp",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SubjectOffering
                {
                    Id = WEB301_Fall2024,
                    SubjectId = WEB301Id,
                    SemesterId = SemesterSeeder.Fall2024Id,
                    MaxClasses = 4,
                    SemesterCapacity = 120,
                    RegistrationStartDate = new DateTime(2024, 7, 1),
                    RegistrationEndDate = new DateTime(2024, 8, 15),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== MATH101: Spring 2024 =====
                new SubjectOffering
                {
                    Id = MATH101_Spring2024,
                    SubjectId = MATH101Id,
                    SemesterId = SemesterSeeder.Spring2024Id,
                    MaxClasses = 10,
                    SemesterCapacity = 400,
                    RegistrationStartDate = new DateTime(2023, 12, 1),
                    RegistrationEndDate = new DateTime(2023, 12, 31),
                    IsActive = true,
                    Notes = "Foundation course - large capacity",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== MATH201: Fall 2024 =====
                new SubjectOffering
                {
                    Id = MATH201_Fall2024,
                    SubjectId = MATH201Id,
                    SemesterId = SemesterSeeder.Fall2024Id,
                    MaxClasses = 5,
                    SemesterCapacity = 200,
                    RegistrationStartDate = new DateTime(2024, 7, 1),
                    RegistrationEndDate = new DateTime(2024, 8, 15),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== CS101: Spring 2024 =====
                new SubjectOffering
                {
                    Id = CS101_Spring2024,
                    SubjectId = CS101Id,
                    SemesterId = SemesterSeeder.Spring2024Id,
                    MaxClasses = 8,
                    SemesterCapacity = 320,
                    RegistrationStartDate = new DateTime(2023, 12, 1),
                    RegistrationEndDate = new DateTime(2023, 12, 31),
                    IsActive = true,
                    Notes = "Intro programming - high enrollment",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== CS201: Summer 2024 =====
                new SubjectOffering
                {
                    Id = CS201_Summer2024,
                    SubjectId = CS201Id,
                    SemesterId = SemesterSeeder.Summer2024Id,
                    MaxClasses = 3,
                    SemesterCapacity = 120,
                    RegistrationStartDate = new DateTime(2024, 5, 1),
                    RegistrationEndDate = new DateTime(2024, 5, 31),
                    IsActive = true,
                    Notes = "Accelerated DSA course",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.SubjectOfferings.AddRangeAsync(offerings);
            await SaveAsync("SubjectOfferings (Semester-Specific)");

            Console.WriteLine($"📊 Created {offerings.Count} subject offerings across multiple semesters");
            Console.WriteLine("Subjects can now be offered in different semesters.");
        }
    }
}
