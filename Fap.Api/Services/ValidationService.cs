using Fap.Api.Interfaces;
using Fap.Domain.Settings;
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
        private readonly IOptionsMonitor<ValidationSettings> _settings;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(
            IMemoryCache cache,
            IOptionsMonitor<ValidationSettings> settings,
            ILogger<ValidationService> logger)
        {
            _cache = cache;
            _settings = settings;
            _logger = logger;
        }

        public bool IsAttendanceDateValidationEnabled =>
            _cache.GetOrCreate(AttendanceDateKey, entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                return _settings.CurrentValue.EnforceAttendanceDateValidation;
            });

        public Task SetAttendanceDateValidationAsync(bool enabled)
        {
            _cache.Set(AttendanceDateKey, enabled, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });

            _logger.LogInformation("Attendance date validation toggled to {Enabled}", enabled);
            return Task.CompletedTask;
        }
    }
}
