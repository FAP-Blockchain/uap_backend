using Fap.Domain.Settings;
using Fap.Infrastructure.Data;
using Fap.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fap.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseAdminController : ControllerBase
    {
        private readonly FapDbContext _context;
        private readonly DatabaseSettings _dbSettings;
        private readonly ILogger<DatabaseAdminController> _logger;

        public DatabaseAdminController(
                  FapDbContext context,
              IOptions<DatabaseSettings> dbSettings,
             ILogger<DatabaseAdminController> logger)
        {
            _context = context;
            _dbSettings = dbSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Reset database - Drop all data, apply migrations, and reseed
        /// ⚠️ DANGER: This will delete ALL data in the database
        /// </summary>
        [HttpPost("reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetDatabase([FromQuery] string confirmationToken)
        {
            if (!_dbSettings.AllowDatabaseAdminApi)
            {
                _logger.LogWarning("Database Admin API is disabled in settings");
                return StatusCode(403, new
                {
                    success = false,
                    message = "Database Admin API is disabled. Enable it in appsettings.json"
                });
            }

            if (confirmationToken != "CONFIRM_RESET_DATABASE")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid confirmation token. Use 'CONFIRM_RESET_DATABASE'"
                });
            }

            try
            {
                _logger.LogWarning("🗑️ Database reset initiated...");

                // ✅ INCREASE TIMEOUT for Azure SQL Database (default is 30s)
                var oldTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(600); // 10 minutes for Azure
                _logger.LogInformation($"⏱️  Command timeout increased: {oldTimeout ?? 30}s → 600s");

                // Step 1: Delete database
                _logger.LogInformation("🗑️  Dropping database...");
                await _context.Database.EnsureDeletedAsync();
                _logger.LogInformation("✅ Database dropped");

                // Step 2: Apply migrations
                _logger.LogInformation("🔄 Applying migrations...");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("✅ Migrations applied");

                // Step 3: Seed data
                _logger.LogInformation("🌱 Seeding data...");
                await DataSeeder.SeedAsync(_context);
                _logger.LogInformation("✅ Data seeded");

                // Restore original timeout
                _context.Database.SetCommandTimeout(oldTimeout);
                _logger.LogInformation($"⏱️  Command timeout restored to {oldTimeout ?? 30}s");

                _logger.LogWarning("✅ Database reset completed successfully!");

                return Ok(new
                {
                    success = true,
                    message = "Database reset successfully!",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Database reset failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Database reset failed: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Apply pending migrations without resetting data
        /// </summary>
        [HttpPost("migrate")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyMigrations()
        {
            if (!_dbSettings.AllowDatabaseAdminApi)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Database Admin API is disabled"
                });
            }

            try
            {
                // ✅ Increase timeout for migrations
                var oldTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(300); // 5 minutes  
                _logger.LogInformation("🔄 Applying pending migrations...");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("✅ Migrations applied successfully");

                _context.Database.SetCommandTimeout(oldTimeout);

                return Ok(new
                {
                    success = true,
                    message = "Migrations applied successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Migration failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Migration failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Reseed data without dropping database (may cause duplicates if data exists)
        /// </summary>
        [HttpPost("reseed")]
        [AllowAnonymous]
        public async Task<IActionResult> ReseedData()
        {
            if (!_dbSettings.AllowDatabaseAdminApi)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Database Admin API is disabled"
                });
            }

            try
            {
                // ✅ Increase timeout for seeding
                var oldTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(300); // 5 minutes

                _logger.LogInformation("🌱 Reseeding data...");
                await DataSeeder.SeedAsync(_context);
                _logger.LogInformation("✅ Data reseeded successfully");

                _context.Database.SetCommandTimeout(oldTimeout);

                return Ok(new
                {
                    success = true,
                    message = "Data reseeded successfully",
                    warning = "This may create duplicate data if it already exists",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Reseeding failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Reseeding failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get database status and pending migrations
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDatabaseStatus()
        {
            if (!_dbSettings.AllowDatabaseAdminApi)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Database Admin API is disabled"
                });
            }

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                return Ok(new
                {
                    success = true,
                    canConnect,
                    pendingMigrations = pendingMigrations.ToList(),
                    appliedMigrations = appliedMigrations.ToList(),
                    hasPendingMigrations = pendingMigrations.Any(),
                    settings = new
                    {
                        autoResetOnStartup = _dbSettings.AutoResetOnStartup,
                        allowDatabaseAdminApi = _dbSettings.AllowDatabaseAdminApi
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get database status");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Failed to get database status: {ex.Message}"
                });
            }
        }
    }
}
