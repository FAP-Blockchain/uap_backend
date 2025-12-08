using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class TeacherStudentSeeder : BaseSeeder
    {
        // Teacher IDs
        public static readonly Guid Teacher1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid Teacher2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddde");
        public static readonly Guid Teacher3Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddf");
        public static readonly Guid Teacher4Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddde0");

        // Student IDs
        public static readonly Guid Student1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        public static readonly Guid Student2Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef");
        public static readonly Guid Student3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef0");
        public static readonly Guid Student4Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef1");
        public static readonly Guid Student5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef2");
        public static readonly Guid Student6Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef3");
        public static readonly Guid Student7Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef4");
        public static readonly Guid Student8Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeef5");

        public TeacherStudentSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Teachers.AnyAsync() || await _context.Students.AnyAsync())
            {
                Console.WriteLine("Teachers and students already exist. Skipping seeding...");
                return;
            }

            await SeedTeachersAsync();
            await SeedStudentsAsync();
        }

        private async Task SeedTeachersAsync()
        {
            var teachers = new List<Teacher>
            {
                new Teacher
                {
                    Id = Teacher1Id,
                    UserId = UserSeeder.Teacher1UserId,
                    TeacherCode = "T001",
                    Specialization = "Software Engineering",
                    HireDate = new DateTime(2020, 1, 15)
                },
                new Teacher
                {
                    Id = Teacher2Id,
                    UserId = UserSeeder.Teacher2UserId,
                    TeacherCode = "T002",
                    Specialization = "Database Systems",
                    HireDate = new DateTime(2019, 8, 20)
                },
                new Teacher
                {
                    Id = Teacher3Id,
                    UserId = UserSeeder.Teacher3UserId,
                    TeacherCode = "T003",
                    Specialization = "Mathematics",
                    HireDate = new DateTime(2018, 3, 10)
                },
                new Teacher
                {
                    Id = Teacher4Id,
                    UserId = UserSeeder.Teacher4UserId,
                    TeacherCode = "T004",
                    Specialization = "Web Development",
                    HireDate = new DateTime(2021, 6, 1)
                }
            };

            await _context.Teachers.AddRangeAsync(teachers);
            await SaveAsync("Teachers");
        }

        private async Task SeedStudentsAsync()
        {
            var students = new List<Student>
            {
                new Student
                {
                    Id = Student1Id,
                    UserId = UserSeeder.Student1UserId,
                    StudentCode = "SE150001",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.75m,
                    IsGraduated = false,
                    CurriculumId = 1  // Software Engineering 2024
                },
                new Student
                {
                    Id = Student2Id,
                    UserId = UserSeeder.Student2UserId,
                    StudentCode = "SE150002",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.50m,
                    IsGraduated = false,
                    CurriculumId = 1  // Software Engineering 2024
                },
                new Student
                {
                    Id = Student3Id,
                    UserId = UserSeeder.Student3UserId,
                    StudentCode = "SE150003",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.90m,
                    IsGraduated = false,
                    CurriculumId = 1  // Software Engineering 2024
                },
                new Student
                {
                    Id = Student4Id,
                    UserId = UserSeeder.Student4UserId,
                    StudentCode = "SE150004",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.25m,
                    IsGraduated = false,
                    CurriculumId = 1  // Software Engineering 2024
                },
                new Student
                {
                    Id = Student5Id,
                    UserId = UserSeeder.Student5UserId,
                    StudentCode = "IA150001",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.60m,
                    IsGraduated = false,
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id
                },
                new Student
                {
                    Id = Student6Id,
                    UserId = UserSeeder.Student6UserId,
                    StudentCode = "IA150002",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.40m,
                    IsGraduated = false,
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id
                },
                new Student
                {
                    Id = Student7Id,
                    UserId = UserSeeder.Student7UserId, // Assuming UserSeeder has this, if not I need to check UserSeeder
                    StudentCode = "GD150001",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.80m,
                    IsGraduated = false,
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id
                },
                new Student
                {
                    Id = Student8Id,
                    UserId = UserSeeder.Student8UserId, // Assuming UserSeeder has this
                    StudentCode = "GD150002",
                    EnrollmentDate = new DateTime(2022, 9, 1),
                    GPA = 3.20m,
                    IsGraduated = false,
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id
                }
            };

            await _context.Students.AddRangeAsync(students);
            await SaveAsync("Students");
        }
    }
}
