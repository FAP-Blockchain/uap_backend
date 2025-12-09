using Fap.Api.Interfaces;
using Fap.Domain.Entities;
using Fap.Domain.Settings;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class ValidationService : IValidationService
    {
        private const string AttendanceDateKey = "validation:attendance-date";

        private readonly IMemoryCache _cache;
        private readonly FapDbContext _context;
        private readonly IOptionsMonitor<ValidationSettings> _settings;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(
            IMemoryCache cache,
            FapDbContext context,
            IOptionsMonitor<ValidationSettings> settings,
            ILogger<ValidationService> logger)
        {
            _cache = cache;
            _context = context;
            _settings = settings;
            _logger = logger;

            // Invalidate cache when settings change so new default is picked up if no DB override exists
            _settings.OnChange(vals =>
            {
                _cache.Remove(AttendanceDateKey);
            });
        }

        public async Task<bool> IsAttendanceDateValidationEnabledAsync()
        {
            if (_cache.TryGetValue(AttendanceDateKey, out bool cachedValue))
            {
                return cachedValue;
            }

            var setting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == AttendanceDateKey);

            var value = setting != null 
                ? bool.Parse(setting.Value) 
                : _settings.CurrentValue.EnforceAttendanceDateValidation;

            _cache.Set(AttendanceDateKey, value, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Cache for 5 mins to reduce DB hits
            });

            return value;
        }

        public async Task SetAttendanceDateValidationAsync(bool enabled)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(x => x.Key == AttendanceDateKey);

            if (setting == null)
            {
                _context.AppSettings.Add(new AppSetting 
                { 
                    Key = AttendanceDateKey, 
                    Value = enabled.ToString(),
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                setting.Value = enabled.ToString();
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _cache.Set(AttendanceDateKey, enabled, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation("Attendance date validation toggled to {Enabled}", enabled);
        }
    }
}
