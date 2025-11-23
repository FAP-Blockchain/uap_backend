using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class RoleSeeder : BaseSeeder
    {
        // Fixed GUIDs for consistency
        public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid TeacherRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid StudentRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public RoleSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Roles.AnyAsync())
            {
                Console.WriteLine("Roles already exist. Skipping seeding...");
                return;
            }

            var adminRole = new Role
            {
                Id = AdminRoleId,
                Name = "Admin"
            };

            var teacherRole = new Role
            {
                Id = TeacherRoleId,
                Name = "Teacher"
            };

            var studentRole = new Role
            {
                Id = StudentRoleId,
                Name = "Student"
            };

            await _context.Roles.AddRangeAsync(adminRole, teacherRole, studentRole);
            await SaveAsync("Roles");

            // Seed basic permissions
            await SeedPermissionsAsync();
        }

        private async Task SeedPermissionsAsync()
        {
            var permissions = new List<Permission>
            {
                // Admin permissions
                new Permission { Id = Guid.NewGuid(), RoleId = AdminRoleId, Code = "ADMIN_ALL", Description = "Full system access" },
           
                // Teacher permissions
                new Permission { Id = Guid.NewGuid(), RoleId = TeacherRoleId, Code = "CLASS_MANAGE", Description = "Manage classes" },
                new Permission { Id = Guid.NewGuid(), RoleId = TeacherRoleId, Code = "ATTENDANCE_TAKE", Description = "Take attendance" },
                new Permission { Id = Guid.NewGuid(), RoleId = TeacherRoleId, Code = "GRADE_SUBMIT", Description = "Submit grades" },
                new Permission { Id = Guid.NewGuid(), RoleId = TeacherRoleId, Code = "SLOT_MANAGE", Description = "Manage slots" },
     
                // Student permissions
                new Permission { Id = Guid.NewGuid(), RoleId = StudentRoleId, Code = "ENROLLMENT_REQUEST", Description = "Request enrollment" },
                new Permission { Id = Guid.NewGuid(), RoleId = StudentRoleId, Code = "GRADE_VIEW", Description = "View grades" },
                new Permission { Id = Guid.NewGuid(), RoleId = StudentRoleId, Code = "SCHEDULE_VIEW", Description = "View schedule" },
                new Permission { Id = Guid.NewGuid(), RoleId = StudentRoleId, Code = "ATTENDANCE_VIEW", Description = "View attendance" }
            };

            await _context.Permissions.AddRangeAsync(permissions);
            await SaveAsync("Permissions");
        }
    }
}
