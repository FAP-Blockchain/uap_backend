using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs; // ServiceResult
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    /// <summary>
    /// Attendance Management API - Quản lý điểm danh sinh viên
    /// </summary>
    [Route("api/attendance")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendanceService attendanceService,
            ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/attendance - Teacher takes attendance for a specific slot
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
#pragma warning disable CS0618 // Type or member is obsolete
        public async Task<IActionResult> TakeAttendance([FromBody] TakeAttendanceRequest request)
        {
            try
            {
                // Get teacher user ID from claims
                var teacherUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

                // Check authorization
                if (!await _attendanceService.CanTakeAttendanceAsync(request.SlotId, teacherUserId))
                {
                    _logger.LogWarning("Teacher {TeacherId} not authorized to take attendance for slot {SlotId}", 
                        teacherUserId, request.SlotId);
                    return Forbid();
                }

                var result = await _attendanceService.TakeAttendanceAsync(request);
#pragma warning restore CS0618 // Type or member is obsolete
                _logger.LogInformation("Attendance taken successfully for slot {SlotId} by teacher {TeacherId}", 
                    request.SlotId, teacherUserId);
                    
                return Ok(new
                {
                    success = true,
                    message = "Attendance taken successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while taking attendance for slot {SlotId}", request.SlotId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking attendance for slot {SlotId}", request.SlotId);
                return StatusCode(500, new { success = false, message = "An error occurred while taking attendance", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/attendance/{id} - Get attendance details by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAttendanceById(Guid id)
        {
            try
            {
                var result = await _attendanceService.GetAttendanceDetailByIdAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("Attendance {AttendanceId} not found", id);
                    return NotFound(new { success = false, message = $"Attendance with ID {id} not found" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance {AttendanceId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/attendance/{id} - Update an existing attendance record
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAttendance(Guid id, [FromBody] UpdateAttendanceRequest request)
        {
            try
            {
                var result = await _attendanceService.UpdateAttendanceAsync(id, request);
                if (result == null)
                {
                    _logger.LogWarning("Attendance {AttendanceId} not found for update", id);
                    return NotFound(new { success = false, message = $"Attendance with ID {id} not found" });
                }
                
                _logger.LogInformation("Attendance {AttendanceId} updated successfully", id);
                return Ok(new
                {
                    success = true,
                    message = "Attendance updated successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance {AttendanceId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating attendance", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/attendance/{id}/excuse - Student requests excuse for absence
        /// </summary>
        [HttpPost("{id}/excuse")]
        [Authorize(Roles = "Student,Admin")]
        public async Task<IActionResult> ExcuseAbsence(Guid id, [FromBody] ExcuseAbsenceRequest request)
        {
            try
            {
                // Get student user ID from claims
                var studentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

                // Check authorization (student can only excuse their own attendance)
                if (User.IsInRole("Student"))
                {
                    if (!await _attendanceService.CanExcuseAbsenceAsync(id, studentUserId))
                    {
                        return Forbid();
                    }
                }

                var result = await _attendanceService.ExcuseAbsenceAsync(id, request);
                if (result == null)
                {
                    return NotFound(new { success = false, message = $"Attendance with ID {id} not found" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Excuse request submitted successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/attendance/filter - Get filtered attendance records with pagination
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> GetAttendancesByFilter([FromQuery] AttendanceFilterRequest filter)
        {
            try
            {
                var result = await _attendanceService.GetAttendancesByFilterAsync(filter);
                return Ok(new
                {
                    success = true,
                    data = result,
                    pageNumber = filter.PageNumber,
                    pageSize = filter.PageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/attendance/{id}/on-chain - Save on-chain info after frontend issues tx
        /// </summary>
        [HttpPost("{id:guid}/on-chain")]
        [Authorize(Roles = "Admin,Teacher,Student")]
        [ProducesResponseType(typeof(ServiceResult<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveAttendanceOnChain(Guid id, [FromBody] SaveAttendanceOnChainRequest request)
        {
            try
            {
                var performedByUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var result = await _attendanceService.SaveAttendanceOnChainAsync(id, request, performedByUserId);
                if (!result.Success)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Status = 400,
                        Title = "Bad Request",
                        Detail = result.Message
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving on-chain info for attendance {AttendanceId}", id);
                return StatusCode(500, new ProblemDetails
                {
                    Status = 500,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while saving on-chain info."
                });
            }
        }

        /// <summary>
        /// GET /api/attendance/verify-list?studentId=...&classId=...
        /// Verify all attendance records of a student in a class by comparing DB with on-chain records.
        /// </summary>
        [HttpGet("verify-list")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyAttendanceList([FromQuery] Guid studentId, [FromQuery] Guid classId)
        {
            try
            {
                var result = await _attendanceService.VerifyAttendanceListAsync(studentId, classId);
                return Ok(new { success = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "VerifyAttendanceList failed for StudentId={StudentId}, ClassId={ClassId}", studentId, classId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attendance list for StudentId={StudentId}, ClassId={ClassId}", studentId, classId);
                return StatusCode(500, new { success = false, message = "An error occurred while verifying attendance list" });
            }
        }
    }
}
