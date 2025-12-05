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
        private readonly ILogger<AuthController> _logger;
        public AuthController(AuthService authService, IOtpService otpService, IEmailService emailService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _otpService = otpService;
            _emailService = emailService;
            _logger = logger;
        }

        // LOGIN
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try
            {
                var result = await _authService.LoginAsync(req);
                if (result == null)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", req.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }
                
                _logger.LogInformation("User {Email} logged in successfully", req.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", req.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        // REFRESH TOKEN
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (result == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token attempt");
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }

        // LOGOUT
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                await _authService.LogoutAsync(userId);
                
                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        // SEND OTP
        [HttpPost("send-otp")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                // Use purpose if provided, otherwise default to "General"
                var purpose = string.IsNullOrEmpty(request.Purpose) ? "General" : request.Purpose;

                var otp = await _otpService.GenerateOtpAsync(request.Email, purpose);
                await _emailService.SendOtpEmailAsync(request.Email, otp, purpose);

                _logger.LogInformation("OTP sent successfully to {Email} for purpose: {Purpose}", request.Email, purpose);
                return Ok(new { message = "OTP sent successfully to your email" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP to {Email}", request.Email);
                return BadRequest(new { message = $"Failed to send OTP: {ex.Message}" });
            }
        }

        // CHANGE PASSWORD WITH OTP (RECOMMENDED)
        [HttpPut("change-password-with-otp")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordWithOtp([FromBody] ChangePasswordWithOtpRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Validate OTP - purpose is ignored, any valid OTP works
                var isValidOtp = await _otpService.ValidateOtpAsync(user.Email, request.OtpCode, "");
                if (!isValidOtp)
                    return BadRequest(new { message = "Invalid or expired OTP" });

                var changePasswordRequest = new ChangePasswordRequest
                {
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword
                };

                var result = await _authService.ChangePasswordAsync(userId, changePasswordRequest);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to change password with OTP: {ex.Message}" });
            }
        }

        // RESET PASSWORD WITH OTP (For users who forgot password - NO LOGIN REQUIRED)
        [HttpPost("reset-password-with-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
        {
            // Validate OTP - purpose is ignored, any valid OTP works
            var isValidOtp = await _otpService.ValidateOtpAsync(request.Email, request.OtpCode, "");
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
        // REGISTER SINGLE ACCOUNT (Admin only)
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            _logger.LogInformation("📝 Registration request for: {Email}", request.Email);

            var result = await _authService.RegisterUserAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("❌ Registration failed: {Message}", result.Message);
                return BadRequest(result);
            }

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(request.Email, request.FullName, request.Password);
                _logger.LogInformation("📧 Welcome email sent to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send welcome email to {Email}", request.Email);
            }

            _logger.LogInformation("✅ User registered successfully: {UserId}", result.UserId);

            return CreatedAtAction(nameof(Register), new { id = result.UserId }, new
            {
                Success = true,
                Message = "User registered successfully",
                Data = new
                {
                    UserId = result.UserId,
                    Email = result.Email,
                    RoleName = result.RoleName,
                    WalletAddress = result.WalletAddress
                },
                BlockchainInfo = result.Blockchain != null ? new
                {
                    WalletAddress = result.Blockchain.WalletAddress,
                    TransactionHash = result.Blockchain.TransactionHash,
                    BlockNumber = result.Blockchain.BlockNumber,
                    RegisteredAt = result.Blockchain.RegisteredAt
                } : null
            });
        }

        /// <summary>
        /// Bulk register users (SQL + Blockchain)
        /// </summary>
        [HttpPost("register/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkRegister([FromBody] BulkRegisterRequest request)
        {
            _logger.LogInformation("📋 Bulk registration request for {Count} users", request.Users.Count);

            var result = await _authService.BulkRegisterAsync(request);

            // Send welcome emails to successful registrations
            foreach (var userResult in result.Results.Where(r => r.Success))
            {
                var userRequest = request.Users.First(u => u.Email == userResult.Email);
                try
                {
                    await _emailService.SendWelcomeEmailAsync(userRequest.Email, userRequest.FullName, userRequest.Password);
                    _logger.LogInformation("📧 Welcome email sent to {Email}", userRequest.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send welcome email to {Email}", userRequest.Email);
                }
            }

            _logger.LogInformation(
                "✅ Bulk registration complete: {Success}/{Total} successful",
                result.SuccessCount,
                result.TotalRequested
            );

            if (result.FailureCount == result.TotalRequested)
            {
                return BadRequest(result);
            }

            return Ok(new
            {
                Success = result.SuccessCount > 0,
                Message = $"Registered {result.SuccessCount}/{result.TotalRequested} users",
                Statistics = new
                {
                    Total = result.TotalRequested,
                    Success = result.SuccessCount,
                    Failed = result.FailureCount
                },
                Results = result.Results.Select(r => new
                {
                    UserId = r.UserId,
                    Email = r.Email,
                    RoleName = r.RoleName,
                    WalletAddress = r.WalletAddress,
                    Success = r.Success,
                    Message = r.Message,
                    Blockchain = r.Blockchain != null ? new
                    {
                        WalletAddress = r.Blockchain.WalletAddress,
                        TransactionHash = r.Blockchain.TransactionHash,
                        BlockNumber = r.Blockchain.BlockNumber,
                        RegisteredAt = r.Blockchain.RegisteredAt
                    } : null
                })
            });
        }
    }
}
