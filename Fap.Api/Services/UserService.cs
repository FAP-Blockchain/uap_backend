using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.User;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Fap.Api.Services
{
    // Ensure the UserService class implements the IUserService interface
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly PasswordHasher<User> _hasher = new();

        public UserService(IUnitOfWork uow, IMapper mapper, ILogger<UserService> logger, ICloudStorageService cloudStorageService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _cloudStorageService = cloudStorageService;
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

                    if (!string.IsNullOrWhiteSpace(request.Specialization))
                    {
                        user.Teacher.Specialization = request.Specialization;
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

        // Update user's blockchain registration info from frontend-provided data
        public async Task<UpdateUserResponse> UpdateUserOnChainAsync(Guid userId, UpdateUserOnChainRequest request)
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

                user.BlockchainTxHash = request.TransactionHash;
                user.BlockNumber = request.BlockNumber;
                user.BlockchainRegisteredAt = request.RegisteredAtUtc ?? DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "User on-chain info updated successfully";
                _logger.LogInformation("User {UserId} blockchain info updated: TxHash={TxHash}, Block={Block}",
                    userId, request.TransactionHash, request.BlockNumber);

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
    }
}