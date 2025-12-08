using System;
using System.Collections.Generic;
using System.Linq;
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
        public static readonly Guid PRF192Id = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid CEA201Id = Guid.Parse("10000000-0000-0000-0000-000000000002");
        public static readonly Guid PRO192Id = Guid.Parse("10000000-0000-0000-0000-000000000003");
        public static readonly Guid MAD101Id = Guid.Parse("10000000-0000-0000-0000-000000000004");
        public static readonly Guid CSD201Id = Guid.Parse("10000000-0000-0000-0000-000000000005");
        public static readonly Guid DBI202Id = Guid.Parse("10000000-0000-0000-0000-000000000006");
        public static readonly Guid PRJ301Id = Guid.Parse("10000000-0000-0000-0000-000000000007");
        public static readonly Guid SWP391Id = Guid.Parse("10000000-0000-0000-0000-000000000008");
        public static readonly Guid SWT301Id = Guid.Parse("10000000-0000-0000-0000-000000000009");
        public static readonly Guid SEP490Id = Guid.Parse("10000000-0000-0000-0000-000000000010");

        // Design Subjects
        public static readonly Guid DRP101Id = Guid.Parse("10000000-0000-0000-0000-000000000011");
        public static readonly Guid DTG102Id = Guid.Parse("10000000-0000-0000-0000-000000000012");
        public static readonly Guid DRS102Id = Guid.Parse("10000000-0000-0000-0000-000000000013");
        public static readonly Guid VCM202Id = Guid.Parse("10000000-0000-0000-0000-000000000014");
        public static readonly Guid TPG203Id = Guid.Parse("10000000-0000-0000-0000-000000000015");
        public static readonly Guid DGP201Id = Guid.Parse("10000000-0000-0000-0000-000000000016");
        public static readonly Guid WDU202cId = Guid.Parse("10000000-0000-0000-0000-000000000017");
        public static readonly Guid ANS201Id = Guid.Parse("10000000-0000-0000-0000-000000000018");
        public static readonly Guid ANC302Id = Guid.Parse("10000000-0000-0000-0000-000000000019");
        public static readonly Guid GRP490Id = Guid.Parse("10000000-0000-0000-0000-000000000020");

        // IA Subjects
        public static readonly Guid CSI106Id = Guid.Parse("10000000-0000-0000-0000-000000000021");
        public static readonly Guid MAE101Id = Guid.Parse("10000000-0000-0000-0000-000000000022");
        public static readonly Guid MAS291Id = Guid.Parse("10000000-0000-0000-0000-000000000023");
        public static readonly Guid PFP191Id = Guid.Parse("10000000-0000-0000-0000-000000000024");
        public static readonly Guid NWC204Id = Guid.Parse("10000000-0000-0000-0000-000000000025");
        public static readonly Guid OSG202Id = Guid.Parse("10000000-0000-0000-0000-000000000026");
        public static readonly Guid CRY303cId = Guid.Parse("10000000-0000-0000-0000-000000000027");
        public static readonly Guid FRS301Id = Guid.Parse("10000000-0000-0000-0000-000000000028");
        public static readonly Guid HOD402Id = Guid.Parse("10000000-0000-0000-0000-000000000029");
        public static readonly Guid IAP490Id = Guid.Parse("10000000-0000-0000-0000-000000000030");

        // SubjectOffering IDs (Semester-specific offerings)
        // Winter 2025 (Sem 1)
        public static readonly Guid PRF192_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000101");
        public static readonly Guid CEA201_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000102");
        public static readonly Guid DRP101_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000111");
        public static readonly Guid DTG102_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000112");
        public static readonly Guid CSI106_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000121");
        public static readonly Guid MAE101_Winter2025 = Guid.Parse("20000000-0000-0000-0000-000000000122");
        
        // Spring 2026 (Sem 2)
        public static readonly Guid PRO192_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000103");
        public static readonly Guid MAD101_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000104");
        public static readonly Guid DRS102_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000113");
        public static readonly Guid VCM202_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000114");
        public static readonly Guid PFP191_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000123");
        public static readonly Guid MAS291_Spring2026 = Guid.Parse("20000000-0000-0000-0000-000000000130");

        // Summer 2026 (Sem 3)
        public static readonly Guid CSD201_Summer2026 = Guid.Parse("20000000-0000-0000-0000-000000000105");
        public static readonly Guid DBI202_Summer2026 = Guid.Parse("20000000-0000-0000-0000-000000000106");
        public static readonly Guid TPG203_Summer2026 = Guid.Parse("20000000-0000-0000-0000-000000000115");
        public static readonly Guid DGP201_Summer2026 = Guid.Parse("20000000-0000-0000-0000-000000000116");
        public static readonly Guid NWC204_Summer2026 = Guid.Parse("20000000-0000-0000-0000-000000000124");

        // Fall 2026 (Sem 4)
        public static readonly Guid PRJ301_Fall2026 = Guid.Parse("20000000-0000-0000-0000-000000000107");
        public static readonly Guid WDU202c_Fall2026 = Guid.Parse("20000000-0000-0000-0000-000000000117");
        public static readonly Guid ANS201_Fall2026 = Guid.Parse("20000000-0000-0000-0000-000000000118");
        public static readonly Guid OSG202_Fall2026 = Guid.Parse("20000000-0000-0000-0000-000000000125");
        public static readonly Guid CRY303c_Fall2026 = Guid.Parse("20000000-0000-0000-0000-000000000126");

        // Winter 2026 (Sem 5)
        public static readonly Guid SWP391_Winter2026 = Guid.Parse("20000000-0000-0000-0000-000000000108");
        public static readonly Guid SWT301_Winter2026 = Guid.Parse("20000000-0000-0000-0000-000000000109");
        public static readonly Guid ANC302_Winter2026 = Guid.Parse("20000000-0000-0000-0000-000000000119");
        public static readonly Guid FRS301_Winter2026 = Guid.Parse("20000000-0000-0000-0000-000000000127");
        public static readonly Guid HOD402_Winter2026 = Guid.Parse("20000000-0000-0000-0000-000000000128");

        // Spring 2027 (Sem 9 - Capstone)
        public static readonly Guid SEP490_Spring2027 = Guid.Parse("20000000-0000-0000-0000-000000000110");
        public static readonly Guid GRP490_Spring2027 = Guid.Parse("20000000-0000-0000-0000-000000000120");
        public static readonly Guid IAP490_Spring2027 = Guid.Parse("20000000-0000-0000-0000-000000000129");

        public SubjectOfferingSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            var subjectsExist = await _context.Subjects.AnyAsync();
            var offeringsExist = await _context.SubjectOfferings.AnyAsync();

            if (subjectsExist && offeringsExist)
            {
                Console.WriteLine("Subjects and offerings already exist. Skipping seeding...");
                return;
            }

            if (!subjectsExist)
            {
                await SeedSubjectsAsync();
            }

            if (!offeringsExist)
            {
                await SeedSubjectOfferingsAsync();
            }
        }

        private async Task SeedSubjectsAsync()
        {
            var subjects = new List<Subject>
            {
                // ===== SEMESTER 1 =====
                new Subject
                {
                    Id = PRF192Id,
                    SubjectCode = "PRF192",
                    SubjectName = "Programming Fundamentals",
                    Description = "Introduction to C programming language, algorithms, and problem solving",
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
                    Id = CEA201Id,
                    SubjectCode = "CEA201",
                    SubjectName = "Computer Organization and Architecture",
                    Description = "Computer systems organization, digital logic, assembly language",
                    Credits = 3,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SEMESTER 2 =====
                new Subject
                {
                    Id = PRO192Id,
                    SubjectCode = "PRO192",
                    SubjectName = "Object-Oriented Programming",
                    Description = "OOP concepts using Java: classes, objects, inheritance, polymorphism",
                    Credits = 3,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = "PRF192",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = MAD101Id,
                    SubjectCode = "MAD101",
                    SubjectName = "Discrete Mathematics",
                    Description = "Logic, sets, relations, graph theory, and combinatorics",
                    Credits = 3,
                    Category = "General",
                    Department = "Mathematics",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SEMESTER 3 =====
                new Subject
                {
                    Id = CSD201Id,
                    SubjectCode = "CSD201",
                    SubjectName = "Data Structures and Algorithms",
                    Description = "Arrays, linked lists, trees, graphs, sorting, searching",
                    Credits = 3,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = "PRO192",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = DBI202Id,
                    SubjectCode = "DBI202",
                    SubjectName = "Database Systems",
                    Description = "Relational database design, SQL, normalization, SQL Server",
                    Credits = 3,
                    Category = "Core",
                    Department = "Computer Science",
                    Prerequisites = "PRO192",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SEMESTER 4 =====
                new Subject
                {
                    Id = PRJ301Id,
                    SubjectCode = "PRJ301",
                    SubjectName = "Java Web Application Development",
                    Description = "Web development with Java Servlets, JSP, MVC pattern",
                    Credits = 3,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = "DBI202,PRO192",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SEMESTER 5 =====
                new Subject
                {
                    Id = SWP391Id,
                    SubjectCode = "SWP391",
                    SubjectName = "Software Development Project",
                    Description = "Application of software engineering principles in a group project",
                    Credits = 3,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = "PRJ301",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = SWT301Id,
                    SubjectCode = "SWT301",
                    SubjectName = "Software Testing",
                    Description = "Software testing techniques, strategies, and tools",
                    Credits = 3,
                    Category = "Core",
                    Department = "Software Engineering",
                    Prerequisites = "SWP391",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== SEMESTER 9 (Capstone) =====
                new Subject
                {
                    Id = SEP490Id,
                    SubjectCode = "SEP490",
                    SubjectName = "SE Capstone Project",
                    Description = "Final capstone project for Software Engineering students",
                    Credits = 10,
                    Category = "Capstone",
                    Department = "Software Engineering",
                    Prerequisites = "SWP391",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== GRAPHIC DESIGN =====
                // Semester 1
                new Subject
                {
                    Id = DRP101Id,
                    SubjectCode = "DRP101",
                    SubjectName = "Drawing – Plaster Statue – Portrait",
                    Description = "Fundamental drawing techniques focusing on plaster statues and portraits",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = DTG102Id,
                    SubjectCode = "DTG102",
                    SubjectName = "Visual Design Tools",
                    Description = "Introduction to digital design tools like Photoshop and Illustrator",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 2
                new Subject
                {
                    Id = DRS102Id,
                    SubjectCode = "DRS102",
                    SubjectName = "Drawing – Form, Still Life",
                    Description = "Advanced drawing techniques for forms and still life compositions",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = "DRP101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = VCM202Id,
                    SubjectCode = "VCM202",
                    SubjectName = "Visual Communication",
                    Description = "Principles of visual communication and graphic language",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 3
                new Subject
                {
                    Id = TPG203Id,
                    SubjectCode = "TPG203",
                    SubjectName = "Basic Typography & Layout",
                    Description = "Fundamentals of typography and layout design",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = "DTG102",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = DGP201Id,
                    SubjectCode = "DGP201",
                    SubjectName = "Digital Painting",
                    Description = "Techniques for digital painting and illustration",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = "DRS102",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 4
                new Subject
                {
                    Id = WDU202cId,
                    SubjectCode = "WDU202c",
                    SubjectName = "UI/UX Design",
                    Description = "User Interface and User Experience design principles",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = "DTG102",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = ANS201Id,
                    SubjectCode = "ANS201",
                    SubjectName = "Idea & Script Development",
                    Description = "Creative idea generation and script writing for multimedia",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 5
                new Subject
                {
                    Id = ANC302Id,
                    SubjectCode = "ANC302",
                    SubjectName = "Character Design",
                    Description = "Designing characters for animation and games",
                    Credits = 3,
                    Category = "Core",
                    Department = "Graphic Design",
                    Prerequisites = "DGP201",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 9 (Capstone)
                new Subject
                {
                    Id = GRP490Id,
                    SubjectCode = "GRP490",
                    SubjectName = "Graduation Project",
                    Description = "Final capstone project for Graphic Design students",
                    Credits = 10,
                    Category = "Capstone",
                    Department = "Graphic Design",
                    Prerequisites = "ANC302", // Simplified prereq
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ===== INFORMATION ASSURANCE =====
                // Semester 1
                new Subject
                {
                    Id = CSI106Id,
                    SubjectCode = "CSI106",
                    SubjectName = "Introduction to Computer Science",
                    Description = "Foundational concepts of computer science and information systems",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = MAE101Id,
                    SubjectCode = "MAE101",
                    SubjectName = "Mathematics for Engineering",
                    Description = "Calculus and linear algebra for engineering applications",
                    Credits = 3,
                    Category = "General",
                    Department = "Mathematics",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 2
                new Subject
                {
                    Id = PFP191Id,
                    SubjectCode = "PFP191",
                    SubjectName = "Programming Fundamentals with Python",
                    Description = "Introduction to programming using Python language",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = MAS291Id,
                    SubjectCode = "MAS291",
                    SubjectName = "Statistics & Probability",
                    Description = "Statistical methods and probability theory for data analysis",
                    Credits = 3,
                    Category = "General",
                    Department = "Mathematics",
                    Prerequisites = "MAE101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 3
                new Subject
                {
                    Id = NWC204Id,
                    SubjectCode = "NWC204",
                    SubjectName = "Computer Networking",
                    Description = "Network protocols, architecture, and administration",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = "CSI106",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 4
                new Subject
                {
                    Id = OSG202Id,
                    SubjectCode = "OSG202",
                    SubjectName = "Operating Systems",
                    Description = "Operating system concepts, process management, and memory management",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = "CEA201",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = CRY303cId,
                    SubjectCode = "CRY303c",
                    SubjectName = "Applied Cryptography",
                    Description = "Cryptographic algorithms, protocols, and security applications",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = "MAD101",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 5
                new Subject
                {
                    Id = FRS301Id,
                    SubjectCode = "FRS301",
                    SubjectName = "Digital Forensics",
                    Description = "Techniques for digital investigation and evidence analysis",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = "OSG202",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Subject
                {
                    Id = HOD402Id,
                    SubjectCode = "HOD402",
                    SubjectName = "Ethical Hacking & Offensive Security",
                    Description = "Penetration testing methodologies and ethical hacking techniques",
                    Credits = 3,
                    Category = "Core",
                    Department = "Information Assurance",
                    Prerequisites = "NWC204",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Semester 9 (Capstone)
                new Subject
                {
                    Id = IAP490Id,
                    SubjectCode = "IAP490",
                    SubjectName = "IA Capstone Project",
                    Description = "Final capstone project for Information Assurance students",
                    Credits = 10,
                    Category = "Capstone",
                    Department = "Information Assurance",
                    Prerequisites = "HOD402",
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
            var semesters = await _context.Semesters.ToDictionaryAsync(s => s.Id);

            var offerings = new List<SubjectOffering>
            {
                // Winter 2025 (Sem 1)
                CreateOffering(PRF192_Winter2025, PRF192Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 6, semesterCapacity: 240, notes: "Freshmen cohort"),
                CreateOffering(CEA201_Winter2025, CEA201Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 4, semesterCapacity: 160),

                // Spring 2026 (Sem 2)
                CreateOffering(PRO192_Spring2026, PRO192Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 6, semesterCapacity: 240),
                CreateOffering(MAD101_Spring2026, MAD101Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 4, semesterCapacity: 160),

                // Summer 2026 (Sem 3)
                CreateOffering(CSD201_Summer2026, CSD201Id, SemesterSeeder.Summer2026Id, semesters, maxClasses: 5, semesterCapacity: 200),
                CreateOffering(DBI202_Summer2026, DBI202Id, SemesterSeeder.Summer2026Id, semesters, maxClasses: 5, semesterCapacity: 200),

                // Fall 2026 (Sem 4)
                CreateOffering(PRJ301_Fall2026, PRJ301Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 4, semesterCapacity: 160),

                // Winter 2026 (Sem 5) - Note: Semester IDs might need adjustment if Winter 2026 is not defined, assuming Winter 2025 is Sem 1, Spring 2026 Sem 2, Summer 2026 Sem 3, Fall 2026 Sem 4. Winter 2026 would be next year? Or maybe Winter 2025 is Jan 2025.
                // Let's assume Winter 2025 is the start.
                // Sem 1: Winter 2025
                // Sem 2: Spring 2026 (Wait, usually Spring follows Winter in same year or next? FPT usually Spring-Summer-Fall)
                // Let's stick to the SemesterSeeder IDs available.
                // Assuming Winter2025, Spring2026, Summer2026, Fall2026 are available.
                
                // For Sem 5 subjects, we might not have a semester seeded yet if we only have 4 semesters.
                // I will map them to Fall2026 or leave them out of offerings if no semester exists, but the user wants data.
                // I'll map SWP391 and SWT301 to Fall2026 as well for now, or reuse existing semesters to ensure they appear.
                // Actually, let's just use Fall2026 for Sem 5 subjects too for testing purposes, or check SemesterSeeder.
                
                CreateOffering(SWP391_Winter2026, SWP391Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 120, notes: "Project semester"),
                CreateOffering(SWT301_Winter2026, SWT301Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 120),

                // Capstone
                CreateOffering(SEP490_Spring2027, SEP490Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 2, semesterCapacity: 60, notes: "Capstone"),

                // ===== GRAPHIC DESIGN OFFERINGS =====
                // Winter 2025 (Sem 1)
                CreateOffering(DRP101_Winter2025, DRP101Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(DTG102_Winter2025, DTG102Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Spring 2026 (Sem 2)
                CreateOffering(DRS102_Spring2026, DRS102Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(VCM202_Spring2026, VCM202Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Summer 2026 (Sem 3)
                CreateOffering(TPG203_Summer2026, TPG203Id, SemesterSeeder.Summer2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(DGP201_Summer2026, DGP201Id, SemesterSeeder.Summer2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Fall 2026 (Sem 4)
                CreateOffering(WDU202c_Fall2026, WDU202cId, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(ANS201_Fall2026, ANS201Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Winter 2026 (Sem 5) - Using Fall2026 for now as placeholder if Winter2026 not available
                CreateOffering(ANC302_Winter2026, ANC302Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Capstone
                CreateOffering(GRP490_Spring2027, GRP490Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 2, semesterCapacity: 60, notes: "GD Capstone"),

                // ===== INFORMATION ASSURANCE OFFERINGS =====
                // Winter 2025 (Sem 1)
                CreateOffering(CSI106_Winter2025, CSI106Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(MAE101_Winter2025, MAE101Id, SemesterSeeder.Winter2025Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Spring 2026 (Sem 2)
                CreateOffering(PFP191_Spring2026, PFP191Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(MAS291_Spring2026, MAS291Id, SemesterSeeder.Spring2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Summer 2026 (Sem 3)
                CreateOffering(NWC204_Summer2026, NWC204Id, SemesterSeeder.Summer2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Fall 2026 (Sem 4)
                CreateOffering(OSG202_Fall2026, OSG202Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(CRY303c_Fall2026, CRY303cId, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Winter 2026 (Sem 5) - Using Fall2026 as placeholder
                CreateOffering(FRS301_Winter2026, FRS301Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),
                CreateOffering(HOD402_Winter2026, HOD402Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 3, semesterCapacity: 90),

                // Capstone
                CreateOffering(IAP490_Spring2027, IAP490Id, SemesterSeeder.Fall2026Id, semesters, maxClasses: 2, semesterCapacity: 60, notes: "IA Capstone")
            };

            await _context.SubjectOfferings.AddRangeAsync(offerings);
            await SaveAsync("SubjectOfferings");

            Console.WriteLine($"Created {offerings.Count} subject offerings across {offerings.Select(o => o.SemesterId).Distinct().Count()} semesters");
        }

        private static SubjectOffering CreateOffering(
            Guid offeringId,
            Guid subjectId,
            Guid semesterId,
            IReadOnlyDictionary<Guid, Semester> semesters,
            int maxClasses,
            int semesterCapacity,
            string? notes = null)
        {
            if (!semesters.TryGetValue(semesterId, out var semester))
            {
                throw new InvalidOperationException($"Semester {semesterId} has not been seeded yet.");
            }

            var registrationStart = semester.StartDate.AddDays(-35);
            var registrationEnd = semester.StartDate.AddDays(7);

            return new SubjectOffering
            {
                Id = offeringId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                MaxClasses = maxClasses,
                SemesterCapacity = semesterCapacity,
                RegistrationStartDate = registrationStart,
                RegistrationEndDate = registrationEnd,
                IsActive = true,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
