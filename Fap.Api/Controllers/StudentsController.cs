using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Enrollment;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.DTOs.Student;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/students")]
    [Authorize]
    public partial class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IGradeService _gradeService;
        private readonly IAttendanceService _attendanceService;
        private readonly IScheduleService _scheduleService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IStudentService studentService,
            IEnrollmentService enrollmentService,
            IGradeService gradeService,
            IAttendanceService attendanceService,
            IScheduleService scheduleService,
            ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _enrollmentService = enrollmentService;
            _gradeService = gradeService;
            _attendanceService = attendanceService;
            _scheduleService = scheduleService;
            _logger = logger;
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// GET /api/students - Get paginated list of all students (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllStudents([FromQuery] GetStudentsRequest request)
        {
            try
            {
                var result = await _studentService.GetStudentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students list");
                return StatusCode(500, new { message = "An error occurred while retrieving students" });
            }
        }

        /// <summary>
        /// GET /api/students/{id} - Get student details by ID (Admin)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);

                if (student == null)
                    return NotFound(new { message = $"Student with ID {id} not found" });

                return Ok(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student {StudentId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving student details" });
            }
        }

        // ==================== HELPER METHODS ====================

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }

        private async Task<Guid> GetCurrentStudentIdAsync()
        {
            var userId = GetCurrentUserId();
            var studentId = await _scheduleService.GetStudentIdByUserIdAsync(userId);
            if (!studentId.HasValue)
            {
                throw new InvalidOperationException("Current user is not a student");
            }
            return studentId.Value;
        }

        // ==================== STUDENT SCHEDULE (Simplified) ====================

        /// <summary>
        /// GET /api/students/me/schedule - Get current student's weekly schedule
        /// If no date provided, returns current week
        /// </summary>
        [HttpGet("me/schedule")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyWeeklySchedule([FromQuery] DateTime? weekStartDate)
        {
            try
            {
                var studentId = await GetCurrentStudentIdAsync();

                // If no date provided, get Monday of current week
                var startDate = weekStartDate ?? GetMonday(DateTime.UtcNow);

                var request = new GetWeeklyScheduleRequest
                {
                    WeekStartDate = startDate,
                    IncludeAttendance = true
                };

                var schedule = await _scheduleService.GetStudentWeeklyScheduleAsync(studentId, request);

                return Ok(new
                {
                    success = true,
                    data = schedule
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student schedule: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET /api/students/me/schedule/statistics - Get schedule statistics for current semester
        /// </summary>
        [HttpGet("me/schedule/statistics")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetScheduleStatistics([FromQuery] Guid? semesterId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var statistics = await _scheduleService.GetScheduleStatisticsAsync(userId, semesterId);

                return Ok(new
                {
                    success = true,
                    data = statistics
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting schedule statistics: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Helper method to get Monday of the week
        private DateTime GetMonday(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // If Sunday (0), go back 6 days, otherwise go back (dayOfWeek - 1) days
            var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            return date.Date.AddDays(-daysToSubtract);
        }

        // ==================== STUDENT SELF-SERVICE ENDPOINTS ====================

        /// <summary>
        /// GET /api/students/me/enrollments - Get my enrollment history (Student)
        /// </summary>
        [HttpGet("me/enrollments")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyEnrollments([FromQuery] GetStudentEnrollmentsRequest request)
        {
            try
            {
                var studentId = await GetCurrentStudentIdAsync();
                var result = await _enrollmentService.GetStudentEnrollmentHistoryAsync(studentId, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my enrollments");
                return StatusCode(500, new { message = "An error occurred while retrieving enrollments" });
            }
        }

        /// <summary>
        /// GET /api/students/me/grades - Get my grade transcript (Student)
        /// </summary>
        [HttpGet("me/grades")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyGrades([FromQuery] GetStudentGradesRequest request)
        {
            try
            {
                var studentId = await GetCurrentStudentIdAsync();
                var result = await _gradeService.GetStudentGradesAsync(studentId, request);

                if (result == null)
                    return NotFound(new { message = "Grade transcript not found" });

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my grades");
                return StatusCode(500, new { message = "An error occurred while retrieving grades" });
            }
        }

        // ==================== ADMIN/TEACHER ENDPOINTS (with {id}) ====================

        /// <summary>
        /// GET /api/students/{id}/enrollments - Get student's enrollment history (Admin/Teacher)
        /// </summary>
        [HttpGet("{id}/enrollments")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetStudentEnrollments(Guid id, [FromQuery] GetStudentEnrollmentsRequest request)
        {
            try
            {
                var result = await _enrollmentService.GetStudentEnrollmentHistoryAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting enrollments for student {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving student enrollments" });
            }
        }

        /// <summary>
        /// GET /api/students/{id}/grades - Get student grade transcript (Admin/Teacher)
        /// </summary>
        [HttpGet("{id}/grades")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetStudentGrades(Guid id, [FromQuery] GetStudentGradesRequest request)
        {
            try
            {
                var result = await _gradeService.GetStudentGradesAsync(id, request);

                if (result == null)
                    return NotFound(new { message = $"Student with ID {id} not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting grades for student {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving student grades" });
            }
        }
    }
}
