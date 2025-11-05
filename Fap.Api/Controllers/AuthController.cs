using Fap.Api.Services;
using Fap.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;

        public AuthController(AuthService authService, IOtpService otpService, IEmailService emailService)
        {
            _authService = authService;
            _otpService = otpService;
            _emailService = emailService;
        }

        // 🔑 LOGIN
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await _authService.LoginAsync(req);
            if (result == null)
                return Unauthorized(new { message = "Invalid email or password" });
            return Ok(result);
        }

        // 🔄 REFRESH TOKEN (NEW)
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            
            if (result == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            
            return Ok(result);
        }

        // 🔓 LOGOUT
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        // 📧 SEND OTP
        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                var otp = await _otpService.GenerateOtpAsync(request.Email, request.Purpose);
                await _emailService.SendOtpEmailAsync(request.Email, otp, request.Purpose);
                
                return Ok(new { message = "OTP sent successfully to your email" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to send OTP: {ex.Message}" });
            }
        }

        // ✅ VERIFY OTP
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var isValid = await _otpService.ValidateOtpAsync(request.Email, request.Code, request.Purpose);
            
            if (!isValid)
                return BadRequest(new { message = "Invalid or expired OTP" });
            
            return Ok(new { message = "OTP verified successfully" });
        }

        // 🔐 CHANGE PASSWORD (NEW)
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Get user ID from JWT token claims
                var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                
                var result = await _authService.ChangePasswordAsync(userId, request);
               
                if (!result.Success)
                    return BadRequest(result);
               
               return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to change password: {ex.Message}" });
            }
        }

        // 🔁 RESET PASSWORD WITH OTP
        [HttpPost("reset-password-with-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
        {
            var isValidOtp = await _otpService.ValidateOtpAsync(request.Email, request.OtpCode, "PasswordReset");
            if (!isValidOtp)
                return BadRequest(new { message = "Invalid or expired OTP" });

            var resetRequest = new ResetPasswordRequest
            {
                Email = request.Email,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            };

            var success = await _authService.ResetPasswordAsync(resetRequest);
            if (!success)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Password reset successfully" });
        }

        // ✅ ĐĂNG KÝ 1 TÀI KHOẢN (Chỉ Admin)
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var result = await _authService.RegisterUserAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            try
            {
                await _emailService.SendWelcomeEmailAsync(request.Email, request.FullName, request.Password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to send welcome email: {ex.Message}");
            }
            
            return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
        }

        // ✅ ĐĂNG KÝ NHIỀU TÀI KHOẢN CÙNG LÚC (Chỉ Admin)
        [HttpPost("register/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkRegister([FromBody] BulkRegisterRequest request)
        {
            var result = await _authService.BulkRegisterAsync(request);
            
            foreach (var userResult in result.Results.Where(r => r.Success))
            {
                var userRequest = request.Users.First(u => u.Email == userResult.Email);
                try
                {
                    await _emailService.SendWelcomeEmailAsync(userRequest.Email, userRequest.FullName, userRequest.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to send welcome email to {userRequest.Email}: {ex.Message}");
                }
            }
            
            if (result.FailureCount == result.TotalRequested)
                return BadRequest(result);
            
            return Ok(result);
        }
    }
}
