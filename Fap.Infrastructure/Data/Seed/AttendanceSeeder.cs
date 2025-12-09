using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Attendance records with various scenarios
    /// </summary>
    public class AttendanceSeeder : BaseSeeder
    {
        public AttendanceSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Attendances.AnyAsync())
      {
      Console.WriteLine("⏭️  Attendances already exist. Skipping...");
        return;
        }

            var attendances = new List<Attendance>();

      // Get all completed slots (past dates)
        var completedSlots = await _context.Slots
    .Where(s => s.Status == "Completed")
    .Include(s => s.Class)
    .ThenInclude(c => c.SubjectOffering)
         .ToListAsync();

     // Get all students
            var students = await _context.Students.ToListAsync();

        var random = new Random(12345); // Fixed seed for consistent data

    foreach (var slot in completedSlots)
            {
   // Get enrolled students for this class
          var classMembers = await _context.ClassMembers
        .Where(cm => cm.ClassId == slot.ClassId)
        .ToListAsync();

     foreach (var member in classMembers)
       {
        // Generate varied attendance patterns (Student1 always present)
        var attendancePattern = member.StudentId == TeacherStudentSeeder.Student1Id
           ? (true, false, null, "Perfect attendance recorded")
           : GetAttendancePattern(random);
    
 var attendance = new Attendance
       {
         Id = Guid.NewGuid(),
   StudentId = member.StudentId,
            SubjectId = slot.Class.SubjectOffering.SubjectId,
   SlotId = slot.Id,
   IsPresent = attendancePattern.IsPresent,
       IsExcused = attendancePattern.IsExcused,
     ExcuseReason = attendancePattern.ExcuseReason,
  Notes = attendancePattern.Notes,
              RecordedAt = slot.Date.AddHours(random.Next(1, 3)) // Recorded during or after class
              // Blockchain fields (left as default for seeded data):
              // OnChainRecordId = null,
              // OnChainTransactionHash = null,
              // IsOnBlockchain = false
         };

         attendances.Add(attendance);
                }
      }

     await _context.Attendances.AddRangeAsync(attendances);
   await SaveAsync("Attendances");

      Console.WriteLine($"   ✅ Created {attendances.Count} attendance records:");
 Console.WriteLine($"      • Present: {attendances.Count(a => a.IsPresent)}");
      Console.WriteLine($"      • Absent (Excused): {attendances.Count(a => !a.IsPresent && a.IsExcused)}");
            Console.WriteLine($"   • Absent (Unexcused): {attendances.Count(a => !a.IsPresent && !a.IsExcused)}");
   }

 private (bool IsPresent, bool IsExcused, string? ExcuseReason, string? Notes) GetAttendancePattern(Random random)
        {
    var roll = random.Next(100);

            if (roll < 80) // 80% present
            {
           return (true, false, null, null);
         }
            else if (roll < 90) // 10% absent but excused
       {
    var excuseReasons = new[]
       {
        "Medical appointment with doctor's note",
               "Family emergency - documented",
            "Attending university-sanctioned event",
       "Illness - medical certificate provided",
   "Religious observance"
  };
          
     return (false, true, excuseReasons[random.Next(excuseReasons.Length)], "Excuse approved by instructor");
            }
  else // 10% absent unexcused
        {
    var notes = random.Next(3) switch
        {
         0 => "No prior notification",
   1 => "Late notification - not approved",
     _ => null
 };
     
  return (false, false, null, notes);
  }
    }
    }
}
