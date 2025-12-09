using System;
using System.Collections.Generic;
using System.Linq;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Slots (class sessions) with various test scenarios
    /// </summary>
    public class SlotSeeder : BaseSeeder
    {
        private const int WeeksPerClass = 8;

        private static readonly ClassSchedule DefaultSchedule = new ClassSchedule(
            DayOfWeek.Monday,
            TimeSlotSeeder.Slot1Id,
            DayOfWeek.Wednesday,
            TimeSlotSeeder.Slot1Id);

        private static readonly IReadOnlyDictionary<Guid, ClassSchedule> ClassSchedules = new Dictionary<Guid, ClassSchedule>
        {
            [ClassSeeder.PRF192_Winter2025_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot1Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot1Id),
            [ClassSeeder.CSI106_Winter2025_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot2Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot2Id),
            [ClassSeeder.MAE101_Winter2025_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot3Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot3Id),
            [ClassSeeder.CEA201_Winter2025_Evening] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot7Id, DayOfWeek.Friday, TimeSlotSeeder.Slot7Id),

            [ClassSeeder.PRO192_Spring2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot1Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot1Id),
            [ClassSeeder.MAD101_Spring2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot4Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot4Id),
            [ClassSeeder.MAS291_Spring2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot3Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot3Id),
            [ClassSeeder.PFP191_Spring2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot2Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot2Id),
            [ClassSeeder.NWC204_Summer2026_A] = new ClassSchedule(DayOfWeek.Friday, TimeSlotSeeder.Slot5Id, DayOfWeek.Saturday, TimeSlotSeeder.Slot6Id),

            [ClassSeeder.PRJ301_Fall2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot1Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot1Id),
            [ClassSeeder.SWP391_Winter2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot4Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot4Id),
            [ClassSeeder.DBI202_Summer2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot5Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot5Id),
            [ClassSeeder.CSD201_Summer2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot6Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot6Id),
            [ClassSeeder.OSG202_Fall2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot6Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot6Id),
            [ClassSeeder.CRY303c_Fall2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot3Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot3Id),

            [ClassSeeder.DRP101_Winter2025_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot2Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot2Id),
            [ClassSeeder.DTG102_Winter2025_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot5Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot5Id),
            [ClassSeeder.DRS102_Spring2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot2Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot2Id),
            [ClassSeeder.VCM202_Spring2026_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot5Id, DayOfWeek.Thursday, TimeSlotSeeder.Slot5Id),

            [ClassSeeder.SWT301_Winter2026_A] = new ClassSchedule(DayOfWeek.Monday, TimeSlotSeeder.Slot4Id, DayOfWeek.Wednesday, TimeSlotSeeder.Slot4Id),
            [ClassSeeder.SEP490_Spring2027_A] = new ClassSchedule(DayOfWeek.Tuesday, TimeSlotSeeder.Slot6Id, DayOfWeek.Friday, TimeSlotSeeder.Slot6Id)
        };

        public SlotSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Slots.AnyAsync())
            {
                Console.WriteLine("Slots already exist. Skipping seeding...");
                return;
            }

            var classes = await _context.Classes
                .Include(c => c.SubjectOffering)
                .ThenInclude(o => o.Semester)
                .Include(c => c.Members)
                .ToListAsync();

            if (!classes.Any())
            {
                Console.WriteLine("No classes found. Skipping slot seeding.");
                return;
            }

            var slots = new List<Slot>();

            foreach (var cls in classes)
            {
                var schedule = ClassSchedules.TryGetValue(cls.Id, out var configured)
                    ? configured
                    : DefaultSchedule;

                var hasStudent1 = cls.Members.Any(m => m.StudentId == TeacherStudentSeeder.Student1Id);

                var semesterStart = cls.SubjectOffering?.Semester?.StartDate.Date ?? DateTime.UtcNow.Date;
                var firstMeeting = AlignToDay(semesterStart, schedule.PrimaryDay);
                var secondMeeting = AlignToDay(semesterStart, schedule.SecondaryDay);

                for (var week = 0; week < WeeksPerClass; week++)
                {
                    var firstDate = firstMeeting.AddDays(week * 7);
                    slots.Add(CreateSlot(cls.Id, firstDate, schedule.PrimarySlotId, hasStudent1));

                    var secondDate = secondMeeting.AddDays(week * 7);
                    slots.Add(CreateSlot(cls.Id, secondDate, schedule.SecondarySlotId, hasStudent1));
                }
            }

            await _context.Slots.AddRangeAsync(slots);
            await SaveAsync("Slots");

            Console.WriteLine($"Created {slots.Count} slots for {classes.Count} classes over {WeeksPerClass} weeks");
        }

        private static Slot CreateSlot(Guid classId, DateTime date, Guid timeSlotId, bool forceCompleted = false)
        {
            var status = (forceCompleted || date.Date < DateTime.UtcNow.Date) ? "Completed" : "Scheduled";

            return new Slot
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                Date = date,
                TimeSlotId = timeSlotId,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static DateTime AlignToDay(DateTime startDate, DayOfWeek targetDay)
        {
            var date = startDate;
            while (date.DayOfWeek != targetDay)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        private sealed record ClassSchedule(DayOfWeek PrimaryDay, Guid PrimarySlotId, DayOfWeek SecondaryDay, Guid SecondarySlotId);
    }
}
