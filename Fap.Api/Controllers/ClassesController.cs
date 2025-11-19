using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Class;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs.Slot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;
        private readonly IGradeService _gradeService;
        private readonly IAttendanceService _attendanceService;
        private readonly ISlotService _slotService;
        private readonly ILogger<ClassesController> _logger;

        public ClassesController(
         IClassService classService,
IGradeService gradeService,
     IAttendanceService attendanceService,
        ISlotService slotService,
            ILogger<ClassesController> logger)
        {
            _classService = classService;
            _gradeService = gradeService;
            _attendanceService = attendanceService;
            _slotService = slotService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of classes with filtering and sorting
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetClasses([FromQuery] GetClassesRequest request)
        {
            try
            {
                var result = await _classService.GetClassesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting classes: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred while retrieving classes", error = ex.Message });
            }
        }

        /// <summary>
        /// Get class by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            try
            {
                var @class = await _classService.GetClassByIdAsync(id);

                if (@class == null)
                    return NotFound(new { message = $"Class with ID {id} not found" });

                return Ok(@class);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting class {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving class" });
            }
        }

        /// <summary>
        /// Create a new class
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            try
            {
                var result = await _classService.CreateClassAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
      nameof(GetClassById),
           new { id = result.ClassId },
        result
              );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating class: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating class" });
            }
        }

        /// <summary>
        /// Update an existing class
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassRequest request)
        {
            try
            {
                var result = await _classService.UpdateClassAsync(id, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating class {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating class" });
            }
        }

        /// <summary>
        /// Delete a class
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            try
            {
                var result = await _classService.DeleteClassAsync(id);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting class {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting class" });
            }
        }

        /// <summary>
        /// GET /api/classes/{id}/roster - Get class roster
        /// </summary>
        [HttpGet("{id}/roster")]
        public async Task<IActionResult> GetClassRoster(Guid id, [FromQuery] ClassRosterRequest request)
        {
            try
            {
                var result = await _classService.GetClassRosterAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting class roster for {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving class roster" });
            }
        }

        /// <summary>
        /// GET /api/classes/{id}/grades - Get class grade report
        /// </summary>
        [HttpGet("{id}/grades")]
        public async Task<IActionResult> GetClassGrades(Guid id, [FromQuery] GetClassGradesRequest request)
        {
            try
            {
                var result = await _gradeService.GetClassGradesAsync(id, request);

                if (result == null)
                    return NotFound(new { message = $"Class with ID {id} not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting class grades for {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving class grades" });
            }
        }

        /// <summary>
        /// GET /api/classes/{id}/slots - Get all slots for a class
        /// </summary>
        [HttpGet("{id}/slots")]
        public async Task<IActionResult> GetClassSlots(Guid id)
        {
            try
            {
                var result = await _slotService.GetSlotsByClassIdAsync(id);
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {result.Count()} slots",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting slots for class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/classes/{id}/attendance - Get attendance history for a class
        /// </summary>
        [HttpGet("{id}/attendance")]
        public async Task<IActionResult> GetClassAttendanceHistory(Guid id)
        {
            try
            {
                var result = await _attendanceService.GetAttendancesByClassIdAsync(id);
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {result.Count()} attendance records",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting attendance for class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/classes/{id}/attendance/report - Get detailed attendance report for a class
        /// </summary>
        [HttpGet("{id}/attendance/report")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetClassAttendanceReport(Guid id)
        {
            try
            {
                var result = await _attendanceService.GetClassAttendanceReportAsync(id);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting attendance report for class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }

        // ==================== ASSIGN STUDENTS TO CLASS ====================

        /// <summary>
        /// GET /api/classes/{id}/students - Get list of students in a class
        /// </summary>
        [HttpGet("{id}/students")]
        public async Task<IActionResult> GetClassStudents(Guid id, [FromQuery] ClassRosterRequest request)
        {
            try
            {
                var result = await _classService.GetClassRosterAsync(id, request);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting students for class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving class students" });
            }
        }

        /// <summary>
        /// POST /api/classes/{id}/students - Assign multiple students to a class
        /// </summary>
        [HttpPost("{id}/students")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignStudentsToClass(Guid id, [FromBody] AssignStudentsRequest request)
        {
            try
            {
                if (request.StudentIds == null || !request.StudentIds.Any())
                {
                    return BadRequest(new { success = false, message = "At least one student ID is required" });
                }

                var result = await _classService.AssignStudentsToClassAsync(id, request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning students to class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while assigning students" });
            }
        }

        /// <summary>
        /// DELETE /api/classes/{id}/students/{studentId} - Remove a student from a class
        /// </summary>
        [HttpDelete("{id}/students/{studentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveStudentFromClass(Guid id, Guid studentId)
        {
            try
            {
                var result = await _classService.RemoveStudentFromClassAsync(id, studentId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing student {studentId} from class {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while removing student" });
            }
        }
    }
}
