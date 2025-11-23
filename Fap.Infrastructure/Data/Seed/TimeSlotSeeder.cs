using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    public class TimeSlotSeeder : BaseSeeder
    {
        public static readonly Guid Slot1Id = Guid.Parse("30000000-0000-0000-0000-000000000001");
        public static readonly Guid Slot2Id = Guid.Parse("30000000-0000-0000-0000-000000000002");
        public static readonly Guid Slot3Id = Guid.Parse("30000000-0000-0000-0000-000000000003");
        public static readonly Guid Slot4Id = Guid.Parse("30000000-0000-0000-0000-000000000004");
        public static readonly Guid Slot5Id = Guid.Parse("30000000-0000-0000-0000-000000000005");
        public static readonly Guid Slot6Id = Guid.Parse("30000000-0000-0000-0000-000000000006");

        public TimeSlotSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.TimeSlots.AnyAsync())
            {
                Console.WriteLine("TimeSlots already exist. Skipping seeding...");
                return;
            }

            var timeSlots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    Id = Slot1Id,
                    Name = "Slot 1",
                    StartTime = new TimeSpan(7, 30, 0),
                    EndTime = new TimeSpan(9, 0, 0)
                },
                new TimeSlot
                {
                    Id = Slot2Id,
                    Name = "Slot 2",
                    StartTime = new TimeSpan(9, 10, 0),
                    EndTime = new TimeSpan(10, 40, 0)
                },
                new TimeSlot
                {
                    Id = Slot3Id,
                    Name = "Slot 3",
                    StartTime = new TimeSpan(10, 50, 0),
                    EndTime = new TimeSpan(12, 20, 0)
                },
                new TimeSlot
                {
                    Id = Slot4Id,
                    Name = "Slot 4",
                    StartTime = new TimeSpan(12, 50, 0),
                    EndTime = new TimeSpan(14, 20, 0)
                },
                new TimeSlot
                {
                    Id = Slot5Id,
                    Name = "Slot 5",
                    StartTime = new TimeSpan(14, 30, 0),
                    EndTime = new TimeSpan(16, 0, 0)
                },
                new TimeSlot
                {
                    Id = Slot6Id,
                    Name = "Slot 6",
                    StartTime = new TimeSpan(16, 10, 0),
                    EndTime = new TimeSpan(17, 40, 0)
                }
            };

            await _context.TimeSlots.AddRangeAsync(timeSlots);
            await SaveAsync("TimeSlots");
        }
    }
}
