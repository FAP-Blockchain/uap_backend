using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Auth;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fap.Api.Services
{
    public class AuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly ILogger<AuthService> _logger;
        private readonly IWalletService _walletService;
        private readonly IBlockchainService _blockchainService;
        private readonly BlockchainSettings _blockchainSettings;

        public AuthService(
            IUnitOfWork uow,
            IConfiguration config,
            IMapper mapper,
            ILogger<AuthService> logger,
            IWalletService walletService,
            IBlockchainService blockchainService,
            IOptions<BlockchainSettings> blockchainSettings)
        {
            _uow = uow;
            _config = config;
            _mapper = mapper;
            _logger = logger;
            _walletService = walletService;
            _blockchainService = blockchainService;
            _blockchainSettings = blockchainSettings.Value;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest req)
        {
            var user = await _uow.Users.GetByEmailAsync(req.Email);

            if (user == null || !user.IsActive) return null;

            if (user.Role == null)
            {
                _logger.LogError("User {Email} has no Role assigned", user.Email);
                throw new Exception($"User {user.Email} không có Role hoặc RoleId trỏ sai.");
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
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }

        // REFRESH TOKEN
        public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshTokenValue)
        {
            var storedToken = (await _uow.RefreshTokens.FindAsync(rt => rt.Token == refreshTokenValue))
                .FirstOrDefault();

            if (storedToken == null) return null;

            if (storedToken.Expires < DateTime.UtcNow)
            {
                _uow.RefreshTokens.Remove(storedToken);
                await _uow.SaveChangesAsync();
                return null;
            }

            var user = await _uow.Users.GetByIdWithDetailsAsync(storedToken.UserId);
            if (user == null || !user.IsActive) return null;

            _uow.RefreshTokens.Remove(storedToken);

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = await CreateRefreshTokenAsync(user);

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                Role = user.Role.Name,
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }

        // LOGOUT
        public async Task<bool> LogoutAsync(Guid userId)
        {
            var tokens = await _uow.RefreshTokens.FindAsync(r => r.UserId == userId);
            foreach (var token in tokens)
                _uow.RefreshTokens.Remove(token);

            await _uow.SaveChangesAsync();
            return true;
        }

        // RESET PASSWORD
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

        // ĐĂNG KÝ 1 TÀI KHOẢN
        public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request)
        {
            var response = new RegisterUserResponse
            {
                Email = request.Email,
                RoleName = request.RoleName
            };

            try
            {
                var teacherSpecializationIds = new List<Guid>();

                var existingUser = await _uow.Users.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    response.Errors.Add($"Email '{request.Email}' already exists");
                    response.Message = "Registration failed";
                    return response;
                }

                var role = await _uow.Roles.GetByNameAsync(request.RoleName);
                if (role == null)
                {
                    response.Errors.Add($"Role '{request.RoleName}' not found");
                    response.Message = "Registration failed";
                    return response;
                }

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

                    // Validate CurriculumId if provided
                    if (request.CurriculumId.HasValue)
                    {
                        var curriculum = await _uow.Curriculums.GetByIdAsync(request.CurriculumId.Value);
                        if (curriculum == null)
                        {
                            response.Errors.Add($"Curriculum with ID '{request.CurriculumId}' not found");
                            response.Message = "Registration failed";
                            return response;
                        }
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

                    teacherSpecializationIds = NormalizeSpecializationIds(request.SpecializationIds);
                    var (specValid, specMessage) = await ValidateSpecializationIdsAsync(teacherSpecializationIds);
                    if (!specValid)
                    {
                        response.Errors.Add(specMessage ?? "Invalid specializations");
                        response.Message = "Registration failed";
                        return response;
                    }
                }

                var walletResult = await _walletService.GetOrCreateWalletAsync(
                    request.WalletAddress,
                    userId: null  // Will associate after user creation
                );

                if (!walletResult.Success || walletResult.Wallet == null)
                {
                    response.Errors.Add("Failed to generate wallet");
                    response.Message = "Registration failed";
                    return response;
                }

                var walletAddress = walletResult.Wallet.Address;
                _logger.LogInformation("Wallet ready: {Address} (IsNew: {IsNew})",
                    walletAddress, walletResult.Wallet.IsNewWallet);

                var user = _mapper.Map<User>(request);
                user.Id = Guid.NewGuid();
                user.PasswordHash = _hasher.HashPassword(user, request.Password);
                user.RoleId = role.Id;
                user.WalletAddress = walletAddress;
                user.CreatedAt = DateTime.UtcNow;

                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("User created with ID: {Id}", user.Id);

                // Associate wallet with user in Wallets table
                if (await _walletService.WalletExistsAsync(walletAddress))
                {
                    await _walletService.AssociateWalletWithUserAsync(walletAddress, user.Id);
                }

                if (request.RoleName.Equals("Student", StringComparison.OrdinalIgnoreCase))
                {
                    var student = _mapper.Map<Student>(request);
                    student.Id = Guid.NewGuid();
                    student.UserId = user.Id;
                    // CurriculumId is mapped by AutoMapper from request.CurriculumId
                    await _uow.Students.AddAsync(student);
                }
                else if (request.RoleName.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                {
                    var teacher = _mapper.Map<Teacher>(request);
                    teacher.Id = Guid.NewGuid();
                    teacher.UserId = user.Id;
                    await _uow.Teachers.AddAsync(teacher);
                    await _uow.Teachers.SetSpecializationsAsync(teacher.Id, teacherSpecializationIds);
                }

                await _uow.SaveChangesAsync();

                BlockchainInfo? blockchainInfo = null;

                // Phương án 2: không đăng ký user on-chain trong backend nữa
                _logger.LogInformation(
                    "User {UserId} registered off-chain only. On-chain registration (if any) will be handled by frontend or a separate flow.",
                    user.Id);

                response.Success = true;
                response.Message = "User registered successfully";
                response.UserId = user.Id;
                response.WalletAddress = walletAddress;
                response.Blockchain = blockchainInfo;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for: {Email}", request.Email);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Registration failed";
                return response;
            }
        }

        // ĐĂNG KÝ NHIỀU TÀI KHOẢN - WITH BLOCKCHAIN SUPPORT
        public async Task<BulkRegisterResponse> BulkRegisterAsync(BulkRegisterRequest request)
        {
            _logger.LogInformation("📋 Bulk registering {Count} users", request.Users.Count);

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

                // Small delay to avoid overwhelming blockchain
                await Task.Delay(500);
            }

            _logger.LogInformation("✅ Bulk registration complete: {Success}/{Total} successful",
                response.SuccessCount, response.TotalRequested);

            return response;
        }

        // CHANGE PASSWORD 
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
                    _logger.LogWarning($"Failed password change attempt for user {userId} - Incorrect current password");
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
                _logger.LogInformation($"User {userId} changed password successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing password for user {userId}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Change password failed";
                return response;
            }
        }

        private static List<Guid> NormalizeSpecializationIds(IEnumerable<Guid>? specializationIds)
        {
            return specializationIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>();
        }

        private async Task<(bool Success, string? Message)> ValidateSpecializationIdsAsync(List<Guid> specializationIds)
        {
            if (!specializationIds.Any())
            {
                return (true, null);
            }

            var existing = await _uow.Specializations.GetByIdsAsync(specializationIds);
            if (existing.Count() != specializationIds.Count)
            {
                return (false, "One or more specialization IDs are invalid");
            }

            return (true, null);
        }

        // ========== Private Helpers ==========

        // GET USER BY ID (Public method for controller use)
        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _uow.Users.GetByIdAsync(userId);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key configuration is missing");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claimsList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            if (user.Student != null)
            {
                claimsList.Add(new Claim("StudentId", user.Student.Id.ToString()));
            }

            if (user.Teacher != null)
            {
                claimsList.Add(new Claim("TeacherId", user.Teacher.Id.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
              audience: _config["Jwt:Audience"],
                claims: claimsList,
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
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };

            await _uow.RefreshTokens.AddAsync(token);
            await _uow.SaveChangesAsync();
            return token;
        }

        // ========== Blockchain Integration Helpers ==========

        /// <summary>
        /// Register user on blockchain smart contract
        /// </summary>
        private async Task<BlockchainInfo?> RegisterOnBlockchainAsync(User user, string walletAddress)
        {
            const string CONTRACT_ABI = @"[{""inputs"":[{""internalType"":""address"",""name"":""_userAddress"",""type"":""address""},{""internalType"":""string"",""name"":""_userId"",""type"":""string""},{""internalType"":""string"",""name"":""_fullName"",""type"":""string""},{""internalType"":""string"",""name"":""_email"",""type"":""string""},{""internalType"":""uint8"",""name"":""_role"",""type"":""uint8""}],""name"":""registerUser"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";

            var contractAddress = _blockchainSettings.Contracts.UniversityManagement;

            if (string.IsNullOrEmpty(contractAddress) || contractAddress == "0x0000000000000000000000000000000000000000")
            {
                _logger.LogWarning("Contract address not configured");
                throw new InvalidOperationException("Blockchain contract not configured");
            }

            var isDeployed = await _blockchainService.IsContractDeployedAsync(contractAddress);
            if (!isDeployed)
            {
                _logger.LogWarning("Contract not deployed at: {Address}", contractAddress);
                throw new InvalidOperationException($"Contract not deployed at {contractAddress}");
            }

            var blockchainRole = MapRoleToBlockchain(user.Role.Name);

            var txHash = await _blockchainService.SendTransactionAsync(
                contractAddress,
                CONTRACT_ABI,
                "registerUser",
                walletAddress,
                user.Id.ToString(),
                user.FullName,
                user.Email,
                blockchainRole
            );

            _logger.LogInformation("Blockchain TX: {TxHash}", txHash);

            var receipt = await _blockchainService.GetTransactionReceiptAsync(txHash);
            if (receipt == null)
                throw new Exception("Failed to get transaction receipt");

            return new BlockchainInfo
            {
                WalletAddress = walletAddress,
                TransactionHash = txHash,
                BlockNumber = (long)receipt.BlockNumber.Value,
                RegisteredAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Map role name to blockchain enum (0=Admin, 1=Teacher, 2=Student)
        /// </summary>
        private int MapRoleToBlockchain(string roleName)
        {
            return roleName.ToLower() switch
            {
                "admin" => 0,
                "teacher" => 1,
                "student" => 2,
                _ => throw new ArgumentException($"Invalid role: {roleName}")
            };
        }
    }
}
