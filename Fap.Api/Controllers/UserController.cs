using Fap.Api.Interfaces;
using Fap.Api.Services;
using Fap.Domain.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}