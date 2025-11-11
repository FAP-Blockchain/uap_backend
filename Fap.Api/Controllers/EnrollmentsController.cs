using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Enrollment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<EnrollmentsController> _logger;

        public EnrollmentsController(
            IEnrollmentService enrollmentService,
            ILogger<EnrollmentsController> logger)
        {
            _enrollmentService = enrollmentService;
            _logger = logger;
        }

        /// POST /api/enrollments - Student enrolls in a class
        [HttpPost]
        public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request)
        {
            try
            {
                var result = await _enrollmentService.CreateEnrollmentAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetEnrollmentById),
                    new { id = result.EnrollmentId },
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating enrollment: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating enrollment" });
            }
        }

        /// GET /api/enrollments/{id} - Get enrollment details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnrollmentById(Guid id)
        {
            try
            {
                var enrollment = await _enrollmentService.GetEnrollmentByIdAsync(id);

                if (enrollment == null)
                    return NotFound(new { message = $"Enrollment with ID {id} not found" });

                return Ok(enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting enrollment {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving enrollment" });
            }
        }

        /// GET /api/enrollments - Get paginated list of enrollments (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEnrollments([FromQuery] GetEnrollmentsRequest request)
        {
            try
            {
                var result = await _enrollmentService.GetEnrollmentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting enrollments: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving enrollments" });
            }
        }

        /// PATCH /api/enrollments/{id}/approve - Admin approves enrollment
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveEnrollment(Guid id)
        {
            try
            {
                var result = await _enrollmentService.ApproveEnrollmentAsync(id);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving enrollment {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while approving enrollment" });
            }
        }

        /// PATCH /api/enrollments/{id}/reject - Admin rejects enrollment
        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectEnrollment(Guid id, [FromBody] RejectEnrollmentRequest? request)
        {
            try
            {
                var result = await _enrollmentService.RejectEnrollmentAsync(id, request?.Reason);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting enrollment {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while rejecting enrollment" });
            }
        }

        /// DELETE /api/enrollments/{id}/drop - Student drops enrollment
        [HttpDelete("{id}/drop")]
        public async Task<IActionResult> DropEnrollment(Guid id)
        {
            try
            {
                // Get student ID from the authenticated user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                // In this case, we need to get the student record for the user
                // For simplicity, assuming userId maps directly to studentId
                // In reality, you may need to query: var student = await _uow.Students.GetByUserIdAsync(userId);
                var studentId = userId; // Adjust based on your user-student relationship

                var result = await _enrollmentService.DropEnrollmentAsync(id, studentId);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error dropping enrollment {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while dropping enrollment" });
            }
        }
    }
}
