using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class SemesterSeeder : BaseSeeder
    {
        public static readonly Guid Spring2024Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        public static readonly Guid Summer2024Id = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffe");
        public static readonly Guid Fall2024Id = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffd");
        public static readonly Guid Spring2025Id = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffc");

        public SemesterSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Semesters.AnyAsync())
            {
                Console.WriteLine("Semesters already exist. Skipping seeding...");
                return;
            }

            var now = DateTime.UtcNow;
            var currentYear = now.Year;

            var semesters = new List<Semester>
            {
                BuildSemester(Spring2024Id, $"Spring {currentYear}", new DateTime(currentYear, 1, 1), new DateTime(currentYear, 5, 31), now),
                BuildSemester(Summer2024Id, $"Summer {currentYear}", new DateTime(currentYear, 6, 1), new DateTime(currentYear, 8, 31), now),
                BuildSemester(Fall2024Id, $"Fall {currentYear}", new DateTime(currentYear, 9, 1), new DateTime(currentYear, 12, 31), now),
                BuildSemester(Spring2025Id, $"Spring {currentYear + 1}", new DateTime(currentYear + 1, 1, 1), new DateTime(currentYear + 1, 5, 31), now)
            };

            await _context.Semesters.AddRangeAsync(semesters);
            await SaveAsync("Semesters");
        }

        private static Semester BuildSemester(Guid id, string name, DateTime start, DateTime end, DateTime now)
        {
            var isCurrent = now >= start && now <= end;

            return new Semester
            {
                Id = id,
                Name = name,
                StartDate = start,
                EndDate = end,
                IsActive = isCurrent,
                IsClosed = now > end,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }
}
