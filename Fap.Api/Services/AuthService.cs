using AutoMapper;
using Fap.Domain.DTOs.Auth;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fap.Api.Services
{
    public class AuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork uow, IConfiguration config, IMapper mapper, ILogger<AuthService> logger)
        {
            _uow = uow;
            _config = config;
            _mapper = mapper;
            _logger = logger;
        }

        // 🔑 LOGIN
        public async Task<LoginResponse?> LoginAsync(LoginRequest req)
        {
            var user = await _uow.Users.GetByEmailAsync(req.Email);

            if (user == null || !user.IsActive) return null;

            if (user.Role == null)
            {
                user = await _uow.Users.GetByIdWithRoleAsync(user.Id);
                if (user?.Role == null)
                    throw new Exception($"User {user?.Email} không có Role hoặc RoleId trỏ sai.");
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed) return null;

            var accessToken = GenerateJwtToken(user);
            var refreshToken = await CreateRefreshTokenAsync(user);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                Role = user.Role.Name,
                FullName = user.FullName
            };
        }

        // 🔄 REFRESH TOKEN
        public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshTokenValue)
        {
            // 1. Validate refresh token
            var storedToken = (await _uow.RefreshTokens.FindAsync(rt => rt.Token == refreshTokenValue))
                .FirstOrDefault();

            if (storedToken == null)
                return null; // Token không tồn tại

            if (storedToken.Expires < DateTime.UtcNow)
            {
                // Token hết hạn, xóa nó đi
                _uow.RefreshTokens.Remove(storedToken);
                await _uow.SaveChangesAsync();
                return null;
            }

            // 2. Lấy user từ refresh token
            var user = await _uow.Users.GetByIdWithRoleAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
                return null;

            // 3. Xóa refresh token cũ
            _uow.RefreshTokens.Remove(storedToken);

            // 4. Tạo access token mới và refresh token mới
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = await CreateRefreshTokenAsync(user);

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                Role = user.Role.Name,
                FullName = user.FullName
            };
        }

        // 🔓 LOGOUT
        public async Task<bool> LogoutAsync(Guid userId)
        {
            var tokens = await _uow.RefreshTokens.FindAsync(r => r.UserId == userId);
            foreach (var token in tokens)
                _uow.RefreshTokens.Remove(token);

            await _uow.SaveChangesAsync();
            return true;
        }

        // 🔁 RESET PASSWORD
        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest req)
        {
            if (req.NewPassword != req.ConfirmPassword)
                throw new Exception("Password confirmation does not match");

            var user = (await _uow.Users.FindAsync(u => u.Email == req.Email)).FirstOrDefault();
            if (user == null) return false;

            user.PasswordHash = _hasher.HashPassword(user, req.NewPassword);
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            return true;
        }

        // ✅ ĐĂNG KÝ 1 TÀI KHOẢN
        public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request)
        {
            var response = new RegisterUserResponse
            {
                Email = request.Email,
                RoleName = request.RoleName
            };

            try
            {
                // 1️⃣ Validate email không trùng
                var existingUser = await _uow.Users.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    response.Errors.Add($"Email '{request.Email}' already exists");
                    response.Message = "Registration failed";
                    return response;
                }

                // 2️⃣ Validate Role
                var role = await _uow.Roles.GetByNameAsync(request.RoleName);
                if (role == null)
                {
                    response.Errors.Add($"Role '{request.RoleName}' not found");
                    response.Message = "Registration failed";
                    return response;
                }

                // 3️⃣ Validate StudentCode/TeacherCode
                if (request.RoleName.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(request.StudentCode))
                    {
                        response.Errors.Add("StudentCode is required for Student role");
                        response.Message = "Registration failed";
                        return response;
                    }

                    var existingStudent = await _uow.Students.GetByStudentCodeAsync(request.StudentCode);
                    if (existingStudent != null)
                    {
                        response.Errors.Add($"StudentCode '{request.StudentCode}' already exists");
                        response.Message = "Registration failed";
                        return response;
                    }
                }
                else if (request.RoleName.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(request.TeacherCode))
                    {
                        response.Errors.Add("TeacherCode is required for Teacher role");
                        response.Message = "Registration failed";
                        return response;
                    }

                    var existingTeacher = await _uow.Teachers.GetByTeacherCodeAsync(request.TeacherCode);
                    if (existingTeacher != null)
                    {
                        response.Errors.Add($"TeacherCode '{request.TeacherCode}' already exists");
                        response.Message = "Registration failed";
                        return response;
                    }
                }

                // 4️⃣ Tạo User bằng AutoMapper
                var user = _mapper.Map<User>(request);
                user.Id = Guid.NewGuid();
                user.PasswordHash = _hasher.HashPassword(null, request.Password);
                user.RoleId = role.Id;

                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();

                // 5️⃣ Tạo Student hoặc Teacher bằng AutoMapper
                if (request.RoleName.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    var student = _mapper.Map<Student>(request);
                    student.Id = Guid.NewGuid();
                    student.UserId = user.Id;

                    await _uow.Students.AddAsync(student);
                }
                else if (request.RoleName.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var teacher = _mapper.Map<Teacher>(request);
                    teacher.Id = Guid.NewGuid();
                    teacher.UserId = user.Id;

                    await _uow.Teachers.AddAsync(teacher);
                }

                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "User registered successfully";
                response.UserId = user.Id;

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Registration failed";
                return response;
            }
        }

        // ✅ ĐĂNG KÝ NHIỀU TÀI KHOẢN
        public async Task<BulkRegisterResponse> BulkRegisterAsync(BulkRegisterRequest request)
        {
            var response = new BulkRegisterResponse
            {
                TotalRequested = request.Users.Count
            };

            foreach (var userRequest in request.Users)
            {
                var result = await RegisterUserAsync(userRequest);
                response.Results.Add(result);

                if (result.Success)
                    response.SuccessCount++;
                else
                    response.FailureCount++;
            }

            return response;
        }

        // 🔐 CHANGE PASSWORD 
        public async Task<ChangePasswordResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var response = new ChangePasswordResponse();

            try
            {
                // 1. Get user
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    response.Errors.Add("User not found");
                    response.Message = "Change password failed";
                    return response;
                }

                // 2. Verify current password
                var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
                if (verifyResult == PasswordVerificationResult.Failed)
                {
                    response.Errors.Add("Current password is incorrect");
                    response.Message = "Change password failed";
                    _logger.LogWarning($"❌ Failed password change attempt for user {userId} - Incorrect current password");
                    return response;
                }

                // 3. Validate new password is different from current
                var isSamePassword = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword);
                if (isSamePassword == PasswordVerificationResult.Success)
                {
                    response.Errors.Add("New password must be different from current password");
                    response.Message = "Change password failed";
                    return response;
                }

                // 4. Hash and update password
                user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Password changed successfully";
                _logger.LogInformation($"✅ User {userId} changed password successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error changing password for user {userId}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Change password failed";
                return response;
            }
        }

        // ========== Private Helpers ==========
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(User user)
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                Expires = DateTime.UtcNow.AddDays(7), // Refresh token hết hạn sau 7 ngày
                UserId = user.Id
            };

            await _uow.RefreshTokens.AddAsync(token);
            await _uow.SaveChangesAsync();
            return token;
        }
    }
}
