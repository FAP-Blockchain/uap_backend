using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds RefreshTokens for JWT authentication flow testing
    /// </summary>
    public class RefreshTokenSeeder : BaseSeeder
    {
        public RefreshTokenSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.RefreshTokens.AnyAsync())
            {
                Console.WriteLine("Refresh tokens already exist. Skipping seeding...");
                return;
            }

            var tokens = new List<RefreshToken>();

            // Get all users
            var users = await _context.Users.ToListAsync();

            if (!users.Any())
            {
                Console.WriteLine("No users found. Skipping refresh tokens seeding...");
                return;
            }

            // ==================== ACTIVE TOKENS ====================
            // Each user gets 1 active token (7 days validity)
            foreach (var user in users)
            {
                tokens.Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    UserId = user.Id
                });
            }

            // ==================== MULTIPLE DEVICE SCENARIOS ====================
            // Some users have multiple active tokens (different devices)
            var usersWithMultipleDevices = users.Take(3).ToList(); // First 3 users
            foreach (var user in usersWithMultipleDevices)
            {
                // Mobile device token
                tokens.Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    UserId = user.Id
                });

                // Tablet device token
                tokens.Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    UserId = user.Id
                });
            }

            // ==================== EXPIRED TOKENS ====================
            // Some users have expired tokens (for testing refresh flow)
            var usersWithExpiredTokens = users.Skip(3).Take(4).ToList();
            foreach (var user in usersWithExpiredTokens)
            {
                tokens.Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(-5), // Expired 5 days ago
                    UserId = user.Id
                });
            }

            // ==================== OLD TOKENS ====================
            // Some very old tokens (should be cleaned up)
            var random = new Random(77777);
            for (int i = 0; i < 3; i++)
            {
                var user = users[random.Next(users.Count)];
                tokens.Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(-random.Next(30, 90)), // 30-90 days old
                    UserId = user.Id
                });
            }

            await _context.RefreshTokens.AddRangeAsync(tokens);
            await SaveAsync("Refresh Tokens");

            Console.WriteLine($"   Created {tokens.Count} refresh tokens:");
            Console.WriteLine($"      • Active tokens: {tokens.Count(t => !t.IsExpired)}");
            Console.WriteLine($"      • Expired tokens: {tokens.Count(t => t.IsExpired)}");
            Console.WriteLine($"      • Users with multiple devices: {usersWithMultipleDevices.Count}");
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
