using Fap.Domain.Constants;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds ActionLogs for audit trail and blockchain tracking
    /// </summary>
    public class ActionLogSeeder : BaseSeeder
    {
        public ActionLogSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.ActionLogs.AnyAsync())
            {
                Console.WriteLine("⏭️  Action Logs already exist. Skipping...");
                return;
            }

            var logs = new List<ActionLog>();
            var random = new Random(88888);

            // Get users for log creation
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == RoleSeeder.AdminRoleId);
            var teachers = await _context.Users.Where(u => u.RoleId == RoleSeeder.TeacherRoleId).ToListAsync();
            var students = await _context.Users.Where(u => u.RoleId == RoleSeeder.StudentRoleId).ToListAsync();

            // Get credentials for credential-related logs
            var credentials = await _context.Credentials.ToListAsync();

            if (adminUser == null || !teachers.Any() || !students.Any())
            {
                Console.WriteLine("⚠️  Required users not found. Skipping action logs...");
                return;
            }

            // ==================== CREDENTIAL ACTIONS ====================
            foreach (var credential in credentials)
            {
                // ISSUE_CREDENTIAL log
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.IssueCredential,
                    Detail = JsonSerializer.Serialize(new
                    {
                        CredentialId = credential.CredentialId,
                        StudentId = credential.StudentId,
                        TemplateId = credential.CertificateTemplateId,
                        IssuedDate = credential.IssuedDate
                    }),
                    UserId = adminUser.Id,
                    CredentialId = credential.Id,
                    CreatedAt = credential.IssuedDate
                });

                // BLOCKCHAIN_STORE log (if on blockchain)
                if (credential.IsOnBlockchain && credential.BlockchainStoredAt.HasValue)
                {
                    logs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        Action = ActionLogActions.BlockchainStore,
                        Detail = JsonSerializer.Serialize(new
                        {
                            CredentialId = credential.CredentialId,
                            TransactionHash = credential.BlockchainTransactionHash,
                            IPFSHash = credential.IPFSHash,
                            StoredAt = credential.BlockchainStoredAt
                        }),
                        UserId = adminUser.Id,
                        CredentialId = credential.Id,
                        CreatedAt = credential.BlockchainStoredAt.Value
                    });
                }

                // VERIFY_CREDENTIAL logs (some credentials were verified)
                if (random.Next(100) < 40) // 40% of credentials were verified
                {
                    logs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        Action = ActionLogActions.VerifyCredential,
                        Detail = JsonSerializer.Serialize(new
                        {
                            CredentialId = credential.CredentialId,
                            VerificationResult = "Valid",
                            BlockchainMatch = credential.IsOnBlockchain
                        }),
                        UserId = students[random.Next(students.Count)].Id,
                        CredentialId = credential.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 15))
                    });
                }

                // REVOKE_CREDENTIAL log (for revoked credentials)
                if (credential.IsRevoked)
                {
                    logs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        Action = ActionLogActions.RevokeCredential,
                        Detail = JsonSerializer.Serialize(new
                        {
                            CredentialId = credential.CredentialId,
                            Reason = "Test revocation scenario",
                            RevokedBy = adminUser.Email
                        }),
                        UserId = adminUser.Id,
                        CredentialId = credential.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(5, 20))
                    });
                }
            }

            // ==================== GRADE ACTIONS ====================
            var grades = await _context.Grades.Take(30).ToListAsync();
            foreach (var grade in grades)
            {
                // SUBMIT_GRADE log
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.SubmitGrade,
                    Detail = JsonSerializer.Serialize(new
                    {
                        StudentId = grade.StudentId,
                        SubjectId = grade.SubjectId,
                        ComponentId = grade.GradeComponentId,
                        Score = grade.Score,
                        LetterGrade = grade.LetterGrade
                    }),
                    UserId = teachers[random.Next(teachers.Count)].Id,
                    CreatedAt = grade.UpdatedAt.AddHours(-random.Next(1, 5))
                });

                // UPDATE_GRADE log (some grades were updated)
                if (random.Next(100) < 20) // 20% grades were updated
                {
                    logs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        Action = ActionLogActions.UpdateGrade,
                        Detail = JsonSerializer.Serialize(new
                        {
                            StudentId = grade.StudentId,
                            ComponentId = grade.GradeComponentId,
                            OldScore = grade.Score.HasValue ? Math.Round(grade.Score.Value - (decimal)(random.NextDouble() * 2), 1) : 0,
                            NewScore = grade.Score ?? 0,
                            Reason = "Score correction after review"
                        }),
                        UserId = teachers[random.Next(teachers.Count)].Id,
                        CreatedAt = grade.UpdatedAt.AddDays(-random.Next(1, 3))
                    });
                }
            }

            // DELETE_GRADE actions (admin deleted wrong grades)
            for (int i = 0; i < 3; i++)
            {
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.DeleteGrade,
                    Detail = JsonSerializer.Serialize(new
                    {
                        Reason = "Incorrect grade entry - duplicate record",
                        DeletedBy = adminUser.Email
                    }),
                    UserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(10, 30))
                });
            }

            // ==================== USER ACTIONS ====================
            // LOGIN actions
            var allUsers = new List<User>();
            allUsers.Add(adminUser);
            allUsers.AddRange(teachers);
            allUsers.AddRange(students);

            foreach (var user in allUsers)
            {
                // Multiple login events
                for (int i = 0; i < random.Next(3, 8); i++)
                {
                    logs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        Action = ActionLogActions.UserLogin,
                        Detail = JsonSerializer.Serialize(new
                        {
                            Email = user.Email,
                            Role = user.RoleId == RoleSeeder.AdminRoleId ? "Admin" :
                user.RoleId == RoleSeeder.TeacherRoleId ? "Teacher" : "Student",
                            IPAddress = $"192.168.1.{random.Next(1, 255)}",
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
                        }),
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    });
                }
            }

            // LOGOUT actions
            for (int i = 0; i < 10; i++)
            {
                var user = allUsers[random.Next(allUsers.Count)];
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.UserLogout,
                    Detail = JsonSerializer.Serialize(new
                    {
                        Email = user.Email,
                        SessionDuration = $"{random.Next(10, 120)} minutes"
                    }),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 20))
                });
            }

            // PASSWORD_RESET actions
            for (int i = 0; i < 3; i++)
            {
                var user = students[random.Next(students.Count)];
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.PasswordReset,
                    Detail = JsonSerializer.Serialize(new
                    {
                        Email = user.Email,
                        ResetMethod = "OTP via Email"
                    }),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(15, 45))
                });
            }

            // USER_CREATED actions
            foreach (var user in allUsers.Take(5))
            {
                logs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    Action = ActionLogActions.UserCreated,
                    Detail = JsonSerializer.Serialize(new
                    {
                        Email = user.Email,
                        Role = user.RoleId == RoleSeeder.AdminRoleId ? "Admin" :
           user.RoleId == RoleSeeder.TeacherRoleId ? "Teacher" : "Student",
                        CreatedBy = adminUser.Email
                    }),
                    UserId = adminUser.Id,
                    CreatedAt = user.CreatedAt
                });
            }

            // ==================== CLASS ACTIONS ====================
            var classes = await _context.Classes
      .Include(c => c.SubjectOffering)
    .ThenInclude(so => so.Subject)
  .ToListAsync();
     
  foreach (var cls in classes)
   {
    // CREATE_CLASS log
              logs.Add(new ActionLog
      {
   Id = Guid.NewGuid(),
             Action = ActionLogActions.CreateClass,
        Detail = JsonSerializer.Serialize(new
           {
   ClassCode = cls.ClassCode,
       SubjectName = cls.SubjectOffering?.Subject?.SubjectName ?? "Unknown",
         MaxEnrollment = cls.MaxEnrollment,
     TeacherUserId = cls.TeacherUserId
   }),
               UserId = adminUser.Id,
       CreatedAt = cls.CreatedAt
              });
            }

         // UPDATE_SCHEDULE actions
  for (int i = 0; i < 5; i++)
            {
 var cls = classes[random.Next(classes.Count)];
    logs.Add(new ActionLog
      {
         Id = Guid.NewGuid(),
                Action = ActionLogActions.UpdateSchedule,
            Detail = JsonSerializer.Serialize(new
      {
     ClassCode = cls.ClassCode,
             UpdateType = "Capacity Change",
       OldCapacity = random.Next(30, 40),
     NewCapacity = cls.MaxEnrollment
           }),
           UserId = adminUser.Id,
   CreatedAt = DateTime.UtcNow.AddDays(-random.Next(5, 25))
           });
   }

            // CANCEL_SLOT actions
        var cancelledSlots = await _context.Slots.Where(s => s.Status == "Cancelled").ToListAsync();
          foreach (var slot in cancelledSlots)
     {
    logs.Add(new ActionLog
   {
            Id = Guid.NewGuid(),
         Action = ActionLogActions.CancelSlot,
   Detail = JsonSerializer.Serialize(new
    {
     SlotId = slot.Id,
                    Date = slot.Date,
                    Reason = slot.Notes
    }),
                    UserId = adminUser.Id,
                    CreatedAt = slot.Date.AddDays(-1) // Cancelled 1 day before
                });
            }

            await _context.ActionLogs.AddRangeAsync(logs);
            await SaveAsync("Action Logs");

            Console.WriteLine($"   ✅ Created {logs.Count} action logs:");
            Console.WriteLine($"      • Credential actions: {logs.Count(l => l.Action.Contains("CREDENTIAL") || l.Action.Contains("BLOCKCHAIN"))}");
            Console.WriteLine($"      • Grade actions: {logs.Count(l => l.Action.Contains("GRADE"))}");
            Console.WriteLine($"      • User actions: {logs.Count(l => l.Action.Contains("USER") || l.Action.Contains("PASSWORD") || l.Action.Contains("LOGIN") || l.Action.Contains("LOGOUT"))}");
            Console.WriteLine($"      • Class actions: {logs.Count(l => l.Action.Contains("CLASS") || l.Action.Contains("SCHEDULE") || l.Action.Contains("SLOT"))}");
        }
    }
}
