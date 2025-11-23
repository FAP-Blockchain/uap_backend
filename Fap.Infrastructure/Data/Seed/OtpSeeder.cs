using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds OTPs for testing password reset and email verification flows
    /// </summary>
    public class OtpSeeder : BaseSeeder
    {
        public OtpSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Otps.AnyAsync())
            {
                Console.WriteLine("OTPs already exist. Skipping seeding...");
                return;
            }

            var otps = new List<Otp>();
            var random = new Random(99999);

            // Get some user emails
            var users = await _context.Users.ToListAsync();

            if (!users.Any())
            {
                Console.WriteLine("No users found. Skipping OTP seeding...");
                return;
            }

            // ==================== ACTIVE OTPs (Not used, not expired) ====================
            // For Password Reset
            for (int i = 0; i < 2; i++)
            {
                var user = users[random.Next(users.Count)];
                otps.Add(new Otp
                {
                    Id = Guid.NewGuid(),
                    Email = user.Email,
                    Code = GenerateOtpCode(),
                    Purpose = "PasswordReset",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2), // Created 2 minutes ago
                    ExpiresAt = DateTime.UtcNow.AddMinutes(3), // 5 minutes validity
                    IsUsed = false,
                    UsedAt = null
                });
            }

            // For Registration (new emails not in system yet)
            otps.Add(new Otp
            {
                Id = Guid.NewGuid(),
                Email = "newuser@fap.edu.vn",
                Code = GenerateOtpCode(),
                Purpose = "Registration",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                ExpiresAt = DateTime.UtcNow.AddMinutes(4),
                IsUsed = false,
                UsedAt = null
            });

            // ==================== USED OTPs (Successfully verified) ====================
            for (int i = 0; i < 4; i++)
            {
                var user = users[random.Next(users.Count)];
                var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 15));
                var usedAt = createdAt.AddMinutes(random.Next(1, 4));

                otps.Add(new Otp
                {
                    Id = Guid.NewGuid(),
                    Email = user.Email,
                    Code = GenerateOtpCode(),
                    Purpose = random.Next(2) == 0 ? "PasswordReset" : "Registration",
                    CreatedAt = createdAt,
                    ExpiresAt = createdAt.AddMinutes(5),
                    IsUsed = true,
                    UsedAt = usedAt
                });
            }

            // ==================== EXPIRED OTPs (Not used but expired) ====================
            for (int i = 0; i < 3; i++)
            {
                var user = users[random.Next(users.Count)];
                var createdAt = DateTime.UtcNow.AddMinutes(-random.Next(10, 30));

                otps.Add(new Otp
                {
                    Id = Guid.NewGuid(),
                    Email = user.Email,
                    Code = GenerateOtpCode(),
                    Purpose = "PasswordReset",
                    CreatedAt = createdAt,
                    ExpiresAt = createdAt.AddMinutes(5), // Already expired
                    IsUsed = false,
                    UsedAt = null
                });
            }

            // ==================== MULTIPLE OTP ATTEMPTS ====================
            // Same user requested OTP multiple times (rate limiting test)
            var targetUser = users.First();
            for (int i = 0; i < 3; i++)
            {
                var createdAt = DateTime.UtcNow.AddMinutes(-i * 2); // 0, 2, 4 minutes ago

                otps.Add(new Otp
                {
                    Id = Guid.NewGuid(),
                    Email = targetUser.Email,
                    Code = GenerateOtpCode(),
                    Purpose = "PasswordReset",
                    CreatedAt = createdAt,
                    ExpiresAt = createdAt.AddMinutes(5),
                    IsUsed = i == 0, // Only the latest one was used
                    UsedAt = i == 0 ? createdAt.AddMinutes(1) : null
                });
            }

            // ==================== DIFFERENT PURPOSE OTPS ====================
            // Email verification OTP
            otps.Add(new Otp
            {
                Id = Guid.NewGuid(),
                Email = "verify@fap.edu.vn",
                Code = GenerateOtpCode(),
                Purpose = "EmailVerification",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                ExpiresAt = DateTime.UtcNow.AddMinutes(4),
                IsUsed = false,
                UsedAt = null
            });

            // Account activation OTP
            otps.Add(new Otp
            {
                Id = Guid.NewGuid(),
                Email = "activate@fap.edu.vn",
                Code = GenerateOtpCode(),
                Purpose = "AccountActivation",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddHours(-1).AddMinutes(5), // Expired
                IsUsed = false,
                UsedAt = null
            });

            await _context.Otps.AddRangeAsync(otps);
            await SaveAsync("OTPs");

            Console.WriteLine($" Created {otps.Count} OTP records:");
            Console.WriteLine($"      • Active (unused, not expired): {otps.Count(o => !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)}");
            Console.WriteLine($"      • Used (verified): {otps.Count(o => o.IsUsed)}");
            Console.WriteLine($"    • Expired (unused): {otps.Count(o => !o.IsUsed && o.ExpiresAt <= DateTime.UtcNow)}");
            Console.WriteLine($"  • Password Reset: {otps.Count(o => o.Purpose == "PasswordReset")}");
            Console.WriteLine($"      • Registration: {otps.Count(o => o.Purpose == "Registration")}");
        }

        /// <summary>
        /// Generates a 6-digit OTP code
        /// </summary>
        private string GenerateOtpCode()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            return random.Next(100000, 999999).ToString();
        }
    }
}
