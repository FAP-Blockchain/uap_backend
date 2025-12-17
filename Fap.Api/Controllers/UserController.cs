using Fap.Api.Interfaces;
using Fap.Api.Services;
using Fap.Domain.DTOs.User;
using Fap.Api.DTOs.User;
using Fap.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of users with filtering and sorting
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")] // Only Admin can view all users
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
        {
            try
            {
                var result = await _userService.GetUsersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Get user by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can view user details
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });
                
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving user" });
            }
        }

        /// <summary>
        /// Update user information
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can update user information
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, request);
                
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating user" });
            }
        }

        /// <summary>
        /// Activate user account
        /// </summary>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")] // Only Admin can activate user accounts
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            try
            {
                var result = await _userService.ActivateUserAsync(id);
                
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error activating user {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while activating user" });
            }
        }

        /// <summary>
        /// Deactivate user account
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")] // Only Admin can deactivate user accounts
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            try
            {
                var result = await _userService.DeactivateUserAsync(id);
                
                if (!result.Success)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating user {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deactivating user" });
            }
        }

        /// <summary>
        /// Upload profile picture for a user (Admin only)
        /// </summary>
        [HttpPost("{id}/profile-picture")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(Guid id, [FromForm] UserProfileImageUploadRequest request)
        {
            try
            {
                var file = request.File;
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Image file is required" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType))
                {
                    return BadRequest(new { message = "Unsupported image format. Please upload JPEG, PNG, or WEBP" });
                }

                // Validate file size (e.g., 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File is too large. Maximum allowed size is 5 MB" });
                }

                await using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var imageUrl = await _userService.UpdateProfileImageAsync(id, memoryStream, file.FileName);

                return Ok(new
                {
                    success = true,
                    message = "Profile picture updated successfully",
                    data = new { imageUrl }
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while uploading the profile picture" });
            }
        }

        /// <summary>
        /// POST /api/user/{id}/on-chain - Persist user's on-chain registration info (Admin only)
        /// </summary>
        [HttpPost("{id}/on-chain")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserOnChain(Guid id, [FromBody] UpdateUserOnChainRequest request)
        {
            try
            {
                var performedByUserId = User.GetRequiredUserId();
                var result = await _userService.UpdateUserOnChainAsync(id, request, performedByUserId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blockchain info for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating user blockchain info" });
            }
        }
    }
}