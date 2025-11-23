using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class UserSeeder : BaseSeeder
    {
        // Fixed GUIDs for users
        public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Teachers
        public static readonly Guid Teacher1UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid Teacher2UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc");
        public static readonly Guid Teacher3UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbd");
        public static readonly Guid Teacher4UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbe");

        // Students
        public static readonly Guid Student1UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid Student2UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccd");
        public static readonly Guid Student3UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccce");
        public static readonly Guid Student4UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccf");
        public static readonly Guid Student5UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccd0");
        public static readonly Guid Student6UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccd1");

        public UserSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                Console.WriteLine("Users already exist. Skipping seeding...");
                return;
            }

            // Pre-compute password hashes
            var adminPasswordHash = HashPassword("Admin@123");
            var teacherPasswordHash = HashPassword("Teacher@123");
            var studentPasswordHash = HashPassword("Student@123");

            var users = new List<User>
            {
                // Admin
                new User
                {
                    Id = AdminUserId,
                    FullName = "System Administrator",
                    Email = "admin@fap.edu.vn",
                    PasswordHash = adminPasswordHash,
                    PhoneNumber = "0900000000",
                    IsActive = true,
                    RoleId = RoleSeeder.AdminRoleId,
                    CreatedAt = DateTime.UtcNow
                },

                // Teachers
                new User
                {
                    Id = Teacher1UserId,
                    FullName = "Nguyễn Văn Thầy",
                    Email = "teacher1@fap.edu.vn",
                    PasswordHash = teacherPasswordHash,
                    PhoneNumber = "0901234567",
                    IsActive = true,
                    RoleId = RoleSeeder.TeacherRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Teacher2UserId,
                    FullName = "Trần Thị Hồng",
                    Email = "teacher2@fap.edu.vn",
                    PasswordHash = teacherPasswordHash,
                    PhoneNumber = "0912345678",
                    IsActive = true,
                    RoleId = RoleSeeder.TeacherRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Teacher3UserId,
                    FullName = "Lê Văn Toán",
                    Email = "teacher3@fap.edu.vn",
                    PasswordHash = teacherPasswordHash,
                    PhoneNumber = "0923456789",
                    IsActive = true,
                    RoleId = RoleSeeder.TeacherRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Teacher4UserId,
                    FullName = "Phạm Thị Mai",
                    Email = "teacher4@fap.edu.vn",
                    PasswordHash = teacherPasswordHash,
                    PhoneNumber = "0934567890",
                    IsActive = true,
                    RoleId = RoleSeeder.TeacherRoleId,
                    CreatedAt = DateTime.UtcNow
                },

                // Students
                new User
                {
                    Id = Student1UserId,
                    FullName = "Nguyễn Văn An",
                    Email = "student1@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0981111111",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Student2UserId,
                    FullName = "Trần Thị Bình",
                    Email = "student2@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0982222222",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Student3UserId,
                    FullName = "Lê Văn Cường",
                    Email = "student3@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0983333333",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Student4UserId,
                    FullName = "Phạm Thị Dung",
                    Email = "student4@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0984444444",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Student5UserId,
                    FullName = "Hoàng Văn Em",
                    Email = "student5@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0985555555",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Student6UserId,
                    FullName = "Vũ Thị Phương",
                    Email = "student6@fap.edu.vn",
                    PasswordHash = studentPasswordHash,
                    PhoneNumber = "0986666666",
                    IsActive = true,
                    RoleId = RoleSeeder.StudentRoleId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Users.AddRangeAsync(users);
            await SaveAsync("Users");
        }
    }
}
