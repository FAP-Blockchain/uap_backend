using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Fap.Api.Services
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string email, string purpose);
        Task<bool> ValidateOtpAsync(string email, string code, string purpose);
        Task CleanupExpiredOtpsAsync();
    }

    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _uow;
        private readonly OtpSettings _otpSettings;
        private readonly ILogger<OtpService> _logger;

        public OtpService(
            IUnitOfWork uow, 
            IOptions<OtpSettings> otpSettings, 
            ILogger<OtpService> logger)
        {
            _uow = uow;
            _otpSettings = otpSettings.Value;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(string email, string purpose)
        {
            // Invalidate old OTPs for same email and purpose
            await _uow.Otps.InvalidateOtpsAsync(email, purpose);
            await _uow.SaveChangesAsync();

            // IMPORTANT: Clear change tracker to prevent Entity Framework tracking conflicts
            // This ensures the new OTP entity is not affected by the previous invalidation
            _uow.ClearChangeTracker();

            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(_otpSettings.ExpirationMinutes);

            // Generate new OTP
            var code = GenerateRandomCode(_otpSettings.Length);
            var otp = new Otp
            {
                Id = Guid.NewGuid(),
                Email = email,
                Code = code,
                Purpose = purpose,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsUsed = false
            };

            await _uow.Otps.AddAsync(otp);
            await _uow.SaveChangesAsync();

            _logger.LogInformation($"OTP generated for {email} - Purpose: {purpose}, Code: {code}, CreatedAt: {now:yyyy-MM-dd HH:mm:ss}, ExpiresAt: {expiresAt:yyyy-MM-dd HH:mm:ss}, IsUsed: {otp.IsUsed}");
            return code;
        }

        public async Task<bool> ValidateOtpAsync(string email, string code, string purpose)
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation($"Validating OTP - Email: {email}, Code: {code}, Purpose: {purpose}, CurrentTime: {now:yyyy-MM-dd HH:mm:ss}");

            var otp = await _uow.Otps.GetValidOtpAsync(email, code, purpose);

            if (otp == null)
            {
                // Debug: Check if OTP exists regardless of expiration
                var allOtps = await _uow.Otps.FindAsync(o => o.Email == email && o.Code == code && o.Purpose == purpose);
                if (allOtps.Any())
                {
                    var foundOtp = allOtps.First();
                    _logger.LogWarning($"OTP exists but invalid - Email: {email}, Code: {code}, IsUsed: {foundOtp.IsUsed}, ExpiresAt: {foundOtp.ExpiresAt:yyyy-MM-dd HH:mm:ss}, CurrentTime: {now:yyyy-MM-dd HH:mm:ss}, Expired: {foundOtp.ExpiresAt < now}");
                }
                else
                {
                    _logger.LogWarning($"OTP not found - Email: {email}, Code: {code}, Purpose: {purpose}");
                }
                return false;
            }

            // Mark as used
            otp.IsUsed = true;
            otp.UsedAt = now;
            _uow.Otps.Update(otp);
            await _uow.SaveChangesAsync();

            _logger.LogInformation($"OTP validated successfully for {email}");
            return true;
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            var expiredOtps = await _uow.Otps.GetExpiredOtpsAsync(7); // Keep for 7 days for audit

            foreach (var otp in expiredOtps)
            {
                _uow.Otps.Remove(otp);
            }

            var deletedCount = await _uow.SaveChangesAsync();
            _logger.LogInformation($"Cleaned up {deletedCount} expired OTPs");
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "0123456789";
            var result = new char[length];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[length];
                rng.GetBytes(buffer);
                
                for (int i = 0; i < length; i++)
                {
                    result[i] = chars[buffer[i] % chars.Length];
                }
            }
            
            return new string(result);
        }
    }
}