using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Slots (class sessions) with various test scenarios
    /// </summary>
    public class SlotSeeder : BaseSeeder
    {
        public SlotSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Slots.AnyAsync())
            {
                Console.WriteLine("Slots already exist. Skipping seeding...");
                return;
            }

            var slots = new List<Slot>();

            // Get all classes to create slots for them
            var classes = await _context.Classes.ToListAsync();
            var timeSlots = await _context.TimeSlots.ToListAsync();

            // Create slots for the past 4 weeks and future 8 weeks (12 weeks total)
            var startDate = DateTime.UtcNow.Date.AddDays(-28); // 4 weeks ago

            foreach (var cls in classes)
            {
                // Each class has 2 sessions per week (e.g., Monday & Thursday)
                for (int week = 0; week < 12; week++)
                {
                    // Session 1: Monday
                    var monday = startDate.AddDays(week * 7);
                    var mondaySlot = new Slot
                    {
                        Id = Guid.NewGuid(),
                        ClassId = cls.Id,
                        Date = monday,
                        TimeSlotId = timeSlots[week % timeSlots.Count].Id, // Rotate through time slots
                        Status = GetSlotStatus(monday),
                        Notes = GetSlotNotes(week),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Session 2: Thursday
                    var thursday = startDate.AddDays(week * 7 + 3);
                    var thursdaySlot = new Slot
                    {
                        Id = Guid.NewGuid(),
                        ClassId = cls.Id,
                        Date = thursday,
                        TimeSlotId = timeSlots[(week + 1) % timeSlots.Count].Id,
                        Status = GetSlotStatus(thursday),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    slots.Add(mondaySlot);
                    slots.Add(thursdaySlot);

                    // Add a cancelled slot for testing (week 6)
                    if (week == 6)
                    {
                        mondaySlot.Status = "Cancelled";
                        mondaySlot.Notes = "National Holiday - No class";
                    }

                    // Add a substitute teacher scenario (week 8)
                    if (week == 8)
                    {
                        var teachers = await _context.Teachers.Where(t => t.UserId != cls.TeacherUserId).ToListAsync();
                        if (teachers.Any())
                        {
                            thursdaySlot.SubstituteTeacherId = teachers.First().Id;
                            thursdaySlot.SubstitutionReason = "Original teacher on conference";
                            thursdaySlot.Notes = "Guest lecturer: Substitute teacher covering advanced topics";
                        }
                    }
                }
            }

            await _context.Slots.AddRangeAsync(slots);
            await SaveAsync("Slots");

            Console.WriteLine($"   Created {slots.Count} slots with various scenarios:");
            Console.WriteLine($" • Completed: {slots.Count(s => s.Status == "Completed")}");
            Console.WriteLine($"      • Scheduled: {slots.Count(s => s.Status == "Scheduled")}");
            Console.WriteLine($"      • Cancelled: {slots.Count(s => s.Status == "Cancelled")}");
            Console.WriteLine($"      • With Substitute Teacher: {slots.Count(s => s.SubstituteTeacherId != null)}");
        }

        private string GetSlotStatus(DateTime date)
        {
            // Past slots are completed, future slots are scheduled
            return date.Date < DateTime.UtcNow.Date ? "Completed" : "Scheduled";
        }

        private string? GetSlotNotes(int week)
        {
            // Add notes for specific weeks
            return week switch
            {
                0 => "Introduction and course overview",
                2 => "Midterm preparation week",
                4 => "Project presentation",
                10 => "Final exam preparation",
                _ => null
            };
        }
    }
}
