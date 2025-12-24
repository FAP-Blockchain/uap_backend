using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.Constants;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.User;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Nethereum.RPC.Eth.DTOs;
using System.Text.Json;
using System.Globalization;
using Fap.Domain.Settings;
using Microsoft.Extensions.Options;
using Nethereum.Util;

namespace Fap.Api.Services
{
    // Ensure the UserService class implements the IUserService interface
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IBlockchainService _blockchain;
        private readonly FapDbContext _db;
        private readonly BlockchainSettings _blockchainSettings;
        private readonly PasswordHasher<User> _hasher = new();

        public UserService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<UserService> logger,
            ICloudStorageService cloudStorageService,
            IBlockchainService blockchain,
            FapDbContext db,
            IOptions<BlockchainSettings> blockchainSettings)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _cloudStorageService = cloudStorageService;
            _blockchain = blockchain;
            _db = db;
            _blockchainSettings = blockchainSettings.Value;
        }

        public async Task<PagedResult<UserResponse>> GetUsersAsync(GetUsersRequest request)
        {
            try
            {
                var (users, totalCount) = await _uow.Users.GetPagedUsersAsync(
                    request.Page,
                    request.PageSize,
                    request.SearchTerm,
                    request.RoleName,
                    request.IsActive,
                    request.SortBy,
                    request.SortOrder
                );

                var userResponses = _mapper.Map<List<UserResponse>>(users);

                return new PagedResult<UserResponse>(
                    userResponses,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error getting users: {ex.Message}");
                throw;
            }
        }

        public async Task<UserResponse?> GetUserByIdAsync(Guid id)
        {
            try
            {
                var user = await _uow.Users.GetByIdWithDetailsAsync(id);
                if (user == null)
                    return null;

                return _mapper.Map<UserResponse>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user {id}: {ex.Message}");
                throw;
            }
        }

    // Update user details
        public async Task<UpdateUserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var response = new UpdateUserResponse
            {
                UserId = id
            };

            try
            {
                // 1. Get existing user with details
                var user = await _uow.Users.GetByIdWithDetailsAsync(id);
                if (user == null)
                {
                    response.Errors.Add($"User with ID {id} not found");
                    response.Message = "Update failed";
                    return response;
                }

                // 2. Update basic user info
                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    user.FullName = request.FullName;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    // Check if email already exists
                    var existingUser = await _uow.Users.GetByEmailAsync(request.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        response.Errors.Add($"Email '{request.Email}' is already taken");
                        response.Message = "Update failed";
                        return response;
                    }
                    user.Email = request.Email;
                }

                if (!string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    user.WalletAddress = request.WalletAddress.Trim();
                }

                // 3. Update role if changed
                if (!string.IsNullOrWhiteSpace(request.RoleName))
                {
                    var newRole = await _uow.Roles.GetByNameAsync(request.RoleName);
                    if (newRole == null)
                    {
                        response.Errors.Add($"Role '{request.RoleName}' not found");
                        response.Message = "Update failed";
                        return response;
                    }

                    if (user.RoleId != newRole.Id)
                    {
                        user.RoleId = newRole.Id;
                    }
                }

                // 4. Update Student info if applicable
                if (user.Student != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.StudentCode) && request.StudentCode != user.Student.StudentCode)
                    {
                        // Check if student code already exists
                        var existingStudent = await _uow.Students.GetByStudentCodeAsync(request.StudentCode);
                        if (existingStudent != null && existingStudent.Id != user.Student.Id)
                        {
                            response.Errors.Add($"StudentCode '{request.StudentCode}' is already taken");
                            response.Message = "Update failed";
                            return response;
                        }
                        user.Student.StudentCode = request.StudentCode;
                    }

                    if (request.EnrollmentDate.HasValue)
                    {
                        user.Student.EnrollmentDate = request.EnrollmentDate.Value;
                    }
                }

                // 5. Update Teacher info if applicable
                if (user.Teacher != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.TeacherCode) && request.TeacherCode != user.Teacher.TeacherCode)
                    {
                        // Check if teacher code already exists
                        var existingTeacher = await _uow.Teachers.GetByTeacherCodeAsync(request.TeacherCode);
                        if (existingTeacher != null && existingTeacher.Id != user.Teacher.Id)
                        {
                            response.Errors.Add($"TeacherCode '{request.TeacherCode}' is already taken");
                            response.Message = "Update failed";
                            return response;
                        }
                        user.Teacher.TeacherCode = request.TeacherCode;
                    }

                    if (request.HireDate.HasValue)
                    {
                        user.Teacher.HireDate = request.HireDate.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        user.PhoneNumber = request.PhoneNumber;
                    }

                    if (request.SpecializationIds != null)
                    {
                        var specializationIds = request.SpecializationIds
                            .Where(id => id != Guid.Empty)
                            .Distinct()
                            .ToList();

                        if (specializationIds.Any())
                        {
                            var specializations = await _uow.Specializations.GetByIdsAsync(specializationIds);
                            if (specializations.Count() != specializationIds.Count)
                            {
                                response.Errors.Add("One or more specialization IDs are invalid");
                                response.Message = "Update failed";
                                return response;
                            }
                        }

                        await _uow.Teachers.SetSpecializationsAsync(user.Teacher.Id, specializationIds);
                    }
                }

                // 6. Save changes
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "User updated successfully";
                _logger.LogInformation($"User {id} updated successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user {id}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Update failed";
                return response;
            }
        }

    // Activate user
        public async Task<UpdateUserResponse> ActivateUserAsync(Guid id)
        {
            var response = new UpdateUserResponse
            {
                UserId = id
            };

            try
            {
                var user = await _uow.Users.GetByIdAsync(id);
                if (user == null)
                {
                    response.Errors.Add($"User with ID {id} not found");
                    response.Message = "Activation failed";
                    return response;
                }

                if (user.IsActive)
                {
                    response.Message = "User is already active";
                    response.Success = true;
                    return response;
                }

                user.IsActive = true;
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "User activated successfully";
                _logger.LogInformation($"User {id} activated successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error activating user {id}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Activation failed";
                return response;
            }
        }

    // Deactivate user
        public async Task<UpdateUserResponse> DeactivateUserAsync(Guid id)
        {
            var response = new UpdateUserResponse
            {
                UserId = id
            };

            try
            {
                var user = await _uow.Users.GetByIdAsync(id);
                if (user == null)
                {
                    response.Errors.Add($"User with ID {id} not found");
                    response.Message = "Deactivation failed";
                    return response;
                }

                if (!user.IsActive)
                {
                    response.Message = "User is already inactive";
                    response.Success = true;
                    return response;
                }

                user.IsActive = false;
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "User deactivated successfully";
                _logger.LogInformation($"User {id} deactivated successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating user {id}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Deactivation failed";
                return response;
            }
        }

        public async Task<string> UpdateProfileImageAsync(Guid userId, Stream imageStream, string fileName)
        {
            try
            {
                if (imageStream == null || imageStream.Length == 0)
                {
                    throw new ArgumentException("Image stream is empty", nameof(imageStream));
                }

                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                var uploadFileName = string.IsNullOrWhiteSpace(fileName)
                    ? $"{user.Id}_profile"
                    : fileName;

                var uploadResult = await _cloudStorageService.UploadProfileImageAsync(imageStream, uploadFileName);

                // Delete previous image if exists and differs
                if (!string.IsNullOrWhiteSpace(user.ProfileImagePublicId) &&
                    !string.Equals(user.ProfileImagePublicId, uploadResult.PublicId, StringComparison.OrdinalIgnoreCase))
                {
                    await _cloudStorageService.DeleteImageAsync(user.ProfileImagePublicId!);
                }

                user.ProfileImageUrl = uploadResult.Url;
                user.ProfileImagePublicId = uploadResult.PublicId;
                user.UpdatedAt = DateTime.UtcNow;

                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                return uploadResult.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile image for user {UserId}", userId);
                throw;
            }
        }

        // Update user's blockchain registration info (backend fetches receipt & decodes logs)
        public async Task<UpdateUserResponse> UpdateUserOnChainAsync(Guid userId, UpdateUserOnChainRequest request, Guid performedByUserId)
        {
            var response = new UpdateUserResponse
            {
                UserId = userId
            };

            try
            {
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    response.Errors.Add($"User with ID {userId} not found");
                    response.Message = "Update failed";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.TransactionHash))
                {
                    response.Errors.Add("TransactionHash is required");
                    response.Message = "Update failed";
                    return response;
                }

                var txHash = request.TransactionHash.Trim();

                // 1) Fetch receipt from chain (do not trust FE-provided block number/time)
                var receipt = await _blockchain.WaitForTransactionReceiptAsync(txHash, timeoutSeconds: 120);
                var blockNumber = receipt.BlockNumber?.Value;
                if (blockNumber == null)
                {
                    response.Errors.Add("Receipt did not include BlockNumber");
                    response.Message = "Update failed";
                    return response;
                }

                // 2) Derive timestamp from block
                DateTime? registeredAtUtc = null;
                try
                {
                    var web3 = _blockchain.GetWeb3();
                    var block = await web3.Eth.Blocks
                        .GetBlockWithTransactionsByNumber
                        .SendRequestAsync(new BlockParameter(receipt.BlockNumber));

                    if (block?.Timestamp?.Value != null)
                    {
                        var unixSeconds = (long)block.Timestamp.Value;
                        registeredAtUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve block timestamp for TxHash={TxHash}", txHash);
                }

                // 3) Decode known events for audit/validation
                var decodedEvents = await _blockchain.DecodeReceiptEventsAsync(txHash);
                var hasExpectedUserEvent = decodedEvents.Any(e =>
                    string.Equals(e.EventName, "UserRegistered", StringComparison.Ordinal) ||
                    string.Equals(e.EventName, "UserUpdated", StringComparison.Ordinal) ||
                    string.Equals(e.EventName, "UserDeactivated", StringComparison.Ordinal));

                if (!hasExpectedUserEvent)
                {
                    response.Errors.Add("Transaction receipt did not contain an expected user event (UserRegistered/UserUpdated/UserDeactivated)");
                    response.Message = "Update failed";
                    return response;
                }

                user.BlockchainTxHash = txHash;
                user.BlockNumber = (long)blockNumber;
                user.BlockchainRegisteredAt = registeredAtUtc ?? DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                // Fetch tx for TxFrom/TxTo (auditability)
                string? txFrom = null;
                string? txTo = null;
                try
                {
                    var web3 = _blockchain.GetWeb3();
                    var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                    txFrom = tx?.From;
                    txTo = tx?.To;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve tx from/to for TxHash={TxHash}", txHash);
                }

                // 4) Persist audit logs linked to this on-chain tx (actor is the admin performing the sync)
                foreach (var e in decodedEvents)
                {
                    string detail;
                    try
                    {
                        using var doc = JsonDocument.Parse(e.DetailJson);
                        var root = doc.RootElement;
                        object? indexedArgs = null;
                        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("indexedArgs", out var ia))
                        {
                            indexedArgs = ia.Clone();
                        }

                        detail = JsonSerializer.Serialize(new
                        {
                            targetUserId = userId,
                            contractAddress = e.ContractAddress,
                            eventName = e.EventName,
                            indexedArgs
                        });
                    }
                    catch
                    {
                        // Fallback: store raw decoded JSON string if parsing fails
                        detail = JsonSerializer.Serialize(new
                        {
                            targetUserId = userId,
                            contractAddress = e.ContractAddress,
                            eventName = e.EventName,
                            decoded = e.DetailJson
                        });
                    }

                    if (detail.Length > 500)
                    {
                        detail = detail.Substring(0, 500);
                    }

                    _db.ActionLogs.Add(new ActionLog
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        Action = ActionLogActions.UserOnChainSync,
                        Detail = detail,
                        UserId = performedByUserId,
                        TransactionHash = txHash,
                        BlockNumber = (long)blockNumber,
                        EventName = e.EventName,
                        TxFrom = txFrom,
                        TxTo = txTo,
                        ContractAddress = e.ContractAddress,
                        CredentialId = null
                    });
                }

                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "User on-chain info updated successfully";
                _logger.LogInformation("User {UserId} blockchain info updated: TxHash={TxHash}, Block={Block}",
                    userId, txHash, (long)blockNumber);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blockchain info for user {UserId}", userId);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Update failed";
                return response;
            }
        }

        private static string DecodeAddressFromAbiWord(string word64)
        {
            // ABI word is 32 bytes (64 hex). Address is last 20 bytes (40 hex)
            if (string.IsNullOrWhiteSpace(word64) || word64.Length < 40)
            {
                return string.Empty;
            }

            var addr = word64[^40..];
            return "0x" + addr.ToLowerInvariant();
        }

        private static bool IsLikelyHexAddress(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            if (!v.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;
            if (v.Length != 42) return false;
            return true;
        }

        public async Task<UpdateUserResponse> UpdateUserWalletOnChainAsync(Guid userId, UpdateUserWalletOnChainRequest request, Guid performedByUserId)
        {
            var response = new UpdateUserResponse
            {
                UserId = userId
            };

            try
            {
                var user = await _uow.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    response.Errors.Add($"User with ID {userId} not found");
                    response.Message = "Update failed";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.TransactionHash))
                {
                    response.Errors.Add("TransactionHash is required");
                    response.Message = "Update failed";
                    return response;
                }

                var contractAddress = _blockchainSettings.Contracts?.UniversityManagement;
                if (string.IsNullOrWhiteSpace(contractAddress))
                {
                    response.Errors.Add("UniversityManagement contract address is not configured (BlockchainSettings.Contracts.UniversityManagement)");
                    response.Message = "Update failed";
                    return response;
                }

                var oldWallet = user.WalletAddress;
                if (!IsLikelyHexAddress(oldWallet))
                {
                    response.Errors.Add("User does not have a valid existing walletAddress to update");
                    response.Message = "Update failed";
                    return response;
                }

                var txHash = request.TransactionHash.Trim();

                // 1) Fetch receipt and tx
                var receipt = await _blockchain.WaitForTransactionReceiptAsync(txHash, timeoutSeconds: 120);
                var blockNumber = receipt.BlockNumber?.Value;
                if (blockNumber == null)
                {
                    response.Errors.Add("Receipt did not include BlockNumber");
                    response.Message = "Update failed";
                    return response;
                }

                var web3 = _blockchain.GetWeb3();
                var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                if (tx == null)
                {
                    response.Errors.Add("Transaction not found on chain");
                    response.Message = "Update failed";
                    return response;
                }

                // 2) Validate tx targets UniversityManagement
                if (!string.Equals(tx.To ?? string.Empty, contractAddress, StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("Transaction was not sent to UniversityManagement contract");
                    response.Message = "Update failed";
                    return response;
                }

                // 3) Validate tx input is updateUserAddress(old,new)
                var input = tx.Input ?? string.Empty;
                if (!input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || input.Length < 10 + 64 * 2)
                {
                    response.Errors.Add("Transaction input is invalid");
                    response.Message = "Update failed";
                    return response;
                }

                var expectedSelector = new Sha3Keccack().CalculateHash("updateUserAddress(address,address)")[..8];
                var actualSelector = input[2..10];
                if (!string.Equals(actualSelector, expectedSelector, StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("Transaction is not calling updateUserAddress(address,address)");
                    response.Message = "Update failed";
                    return response;
                }

                var payload = input[10..];
                var word1 = payload[..64];
                var word2 = payload[64..128];

                var decodedOld = DecodeAddressFromAbiWord(word1);
                var decodedNew = DecodeAddressFromAbiWord(word2);

                if (!IsLikelyHexAddress(decodedNew))
                {
                    response.Errors.Add("Decoded new wallet address is invalid");
                    response.Message = "Update failed";
                    return response;
                }

                if (!string.Equals(decodedOld, oldWallet, StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("Decoded old wallet address does not match user's current walletAddress");
                    response.Message = "Update failed";
                    return response;
                }

                if (string.Equals(decodedOld, decodedNew, StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("New wallet address must be different from old wallet address");
                    response.Message = "Update failed";
                    return response;
                }

                // 4) Validate on-chain state updated: userIdToAddress(userIdString) == newAddress
                const string UniversityManagementUserIdToAddressAbi = "[{ 'inputs':[{'internalType':'string','name':'','type':'string'}], 'name':'userIdToAddress', 'outputs':[{'internalType':'address','name':'','type':'address'}], 'stateMutability':'view', 'type':'function' }]";
                var contract = web3.Eth.GetContract(UniversityManagementUserIdToAddressAbi, contractAddress);
                var fn = contract.GetFunction("userIdToAddress");

                var userIdString = userId.ToString();
                var onchainAddress = await fn.CallAsync<string>(userIdString);
                if (!string.Equals(onchainAddress ?? string.Empty, decodedNew, StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("On-chain state does not reflect the new wallet address for this userId");
                    response.Message = "Update failed";
                    return response;
                }

                // 5) Update DB + audit
                DateTime? blockUtc = null;
                try
                {
                    var block = await web3.Eth.Blocks
                        .GetBlockWithTransactionsByNumber
                        .SendRequestAsync(new BlockParameter(receipt.BlockNumber));
                    if (block?.Timestamp?.Value != null)
                    {
                        var unixSeconds = (long)block.Timestamp.Value;
                        blockUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve block timestamp for wallet update TxHash={TxHash}", txHash);
                }

                user.WalletAddress = decodedNew;
                user.BlockchainTxHash = txHash;
                user.BlockNumber = (long)blockNumber;
                user.BlockchainRegisteredAt = blockUtc ?? DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                var detail = JsonSerializer.Serialize(new
                {
                    targetUserId = userId,
                    oldWalletAddress = decodedOld,
                    newWalletAddress = decodedNew,
                    contractAddress,
                    transactionHash = txHash
                });

                if (detail.Length > 500)
                {
                    detail = detail.Substring(0, 500);
                }

                _db.ActionLogs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    Action = ActionLogActions.UserWalletOnChainUpdate,
                    Detail = detail,
                    UserId = performedByUserId,
                    TransactionHash = txHash,
                    BlockNumber = (long)blockNumber,
                    EventName = "UserWalletUpdated",
                    TxFrom = tx.From,
                    TxTo = tx.To,
                    ContractAddress = contractAddress,
                    CredentialId = null
                });

                await _db.SaveChangesAsync();

                response.Success = true;
                response.Message = "User wallet address updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wallet on-chain for user {UserId}", userId);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Update failed";
                return response;
            }
        }
    }
}