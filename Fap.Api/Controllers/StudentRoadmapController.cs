using Fap.Api.Interfaces;
using Fap.Domain.DTOs.StudentRoadmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class StudentRoadmapController : ControllerBase
    {
        private readonly IStudentRoadmapService _roadmapService;
        private readonly ILogger<StudentRoadmapController> _logger;

        public StudentRoadmapController(
            IStudentRoadmapService roadmapService,
            ILogger<StudentRoadmapController> logger)
        {
            _roadmapService = roadmapService;
            _logger = logger;
        }

        // ==================== STUDENT SELF-SERVICE APIs ====================

        [HttpGet("api/students/me/roadmap")]
        public async Task<IActionResult> GetMyRoadmap()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var roadmap = await _roadmapService.GetMyRoadmapAsync(studentId);

                if (roadmap == null)
                    return NotFound(new { message = "No roadmap found" });

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student roadmap");
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpGet("api/students/me/curriculum-roadmap")]
        public async Task<IActionResult> GetMyCurriculumRoadmap()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var roadmap = await _roadmapService.GetCurriculumRoadmapAsync(studentId);
                if (roadmap == null)
                    return NotFound(new { message = "Curriculum roadmap not available" });

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curriculum roadmap");
                return StatusCode(500, new { message = "An error occurred while retrieving curriculum roadmap" });
            }
        }

        [HttpGet("api/students/me/curriculum-roadmap/summary")]
        public async Task<IActionResult> GetMyCurriculumRoadmapSummary()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var summary = await _roadmapService.GetCurriculumRoadmapSummaryAsync(studentId);
                if (summary == null)
                    return NotFound(new { message = "Curriculum roadmap not available" });

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curriculum roadmap summary");
                return StatusCode(500, new { message = "An error occurred while retrieving curriculum roadmap summary" });
            }
        }

        [HttpGet("api/students/me/curriculum-roadmap/semesters")]
        public async Task<IActionResult> GetMyCurriculumRoadmapSemester([FromQuery] int semesterNumber)
        {
            try
            {
                if (semesterNumber <= 0)
                    return BadRequest(new { message = "Semester number must be greater than zero" });

                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var semester = await _roadmapService.GetCurriculumRoadmapSemesterAsync(studentId, semesterNumber);
                if (semester == null)
                    return NotFound(new { message = "No roadmap data for the requested semester" });

                return Ok(semester);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curriculum roadmap semester detail");
                return StatusCode(500, new { message = "An error occurred while retrieving curriculum roadmap semester detail" });
            }
        }

        [HttpGet("api/students/me/graduation-status")]
        public async Task<IActionResult> GetMyGraduationStatus()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var status = await _roadmapService.EvaluateGraduationEligibilityAsync(studentId, persistIfEligible: true);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating graduation status");
                return StatusCode(500, new { message = "An error occurred while evaluating graduation status" });
            }
        }

        [HttpGet("api/students/me/roadmap/semesters/{semesterId}")]
        public async Task<IActionResult> GetMyRoadmapBySemester(Guid semesterId)
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var roadmap = await _roadmapService.GetRoadmapBySemesterAsync(studentId, semesterId);
                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap by semester");
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpGet("api/students/me/roadmap/current")]
        public async Task<IActionResult> GetMyCurrentSemesterRoadmap()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var roadmap = await _roadmapService.GetCurrentSemesterRoadmapAsync(studentId);
                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current semester roadmap");
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpGet("api/students/me/roadmap/recommendations")]
        public async Task<IActionResult> GetMyRecommendedSubjects()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var recommendations = await _roadmapService.GetRecommendedSubjectsAsync(studentId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended subjects");
                return StatusCode(500, new { message = "An error occurred while retrieving recommendations" });
            }
        }

        [HttpGet("api/students/me/roadmap/retake-options")]
        public async Task<IActionResult> GetMyRetakeOptions()
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var retakes = await _roadmapService.GetRetakeOptionsAsync(studentId);
                return Ok(retakes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retake options");
                return StatusCode(500, new { message = "An error occurred while retrieving retake options" });
            }
        }

        [HttpGet("api/students/me/roadmap/paged")]
        public async Task<IActionResult> GetMyPagedRoadmap([FromQuery] GetStudentRoadmapRequest request)
        {
            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var result = await _roadmapService.GetPagedRoadmapAsync(studentId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged roadmap");
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpPost("api/students/me/roadmap/{roadmapId:guid}/retake-plan")]
        public async Task<IActionResult> PlanMyRetake(Guid roadmapId, [FromBody] PlanRetakeRequest request)
        {
            if (request == null || request.SemesterId == Guid.Empty)
            {
                return BadRequest(new { message = "SemesterId is required" });
            }

            try
            {
                var studentId = GetStudentIdFromToken();
                if (studentId == Guid.Empty)
                    return BadRequest(new { message = "Student ID not found in token" });

                var result = await _roadmapService.PlanRetakeAsync(studentId, roadmapId, request);
                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error planning retake");
                return StatusCode(500, new { message = "An error occurred while planning retake" });
            }
        }

        // ==================== ADMIN APIs ====================

        [HttpGet("api/students/{studentId}/roadmap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentRoadmap(Guid studentId)
        {
            try
            {
                var roadmap = await _roadmapService.GetMyRoadmapAsync(studentId);

                if (roadmap == null)
                    return NotFound(new { message = "No roadmap found for this student" });

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpGet("api/students/{studentId}/curriculum-roadmap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentCurriculumRoadmap(Guid studentId)
        {
            try
            {
                var roadmap = await _roadmapService.GetCurriculumRoadmapAsync(studentId);
                if (roadmap == null)
                    return NotFound(new { message = "Curriculum roadmap not available for this student" });

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting curriculum roadmap");
                return StatusCode(500, new { message = "An error occurred while retrieving curriculum roadmap" });
            }
        }

        [HttpPost("api/students/{studentId}/graduation/evaluate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EvaluateGraduationStatus(Guid studentId, [FromBody] EvaluateGraduationRequest? request)
        {
            try
            {
                var persist = request?.MarkAsGraduated ?? true;
                var status = await _roadmapService.EvaluateGraduationEligibilityAsync(studentId, persist);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating graduation status");
                return StatusCode(500, new { message = "An error occurred while evaluating graduation status" });
            }
        }

        [HttpGet("api/students/{studentId}/roadmap/semesters/{semesterId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentRoadmapBySemester(Guid studentId, Guid semesterId)
        {
            try
            {
                var roadmap = await _roadmapService.GetRoadmapBySemesterAsync(studentId, semesterId);
                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpGet("api/roadmap/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoadmapById(Guid id)
        {
            try
            {
                var roadmap = await _roadmapService.GetRoadmapByIdAsync(id);

                if (roadmap == null)
                    return NotFound(new { message = $"Roadmap entry with ID {id} not found" });

                return Ok(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap {RoadmapId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
        }

        [HttpPost("api/students/{studentId}/roadmap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoadmapEntry(Guid studentId, [FromBody] CreateStudentRoadmapRequest request)
        {
            try
            {
                request.StudentId = studentId;
                var result = await _roadmapService.CreateRoadmapAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetRoadmapById),
                    new { id = result.RoadmapId },
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating roadmap");
                return StatusCode(500, new { message = "An error occurred while creating roadmap" });
            }
        }

        [HttpPost("api/students/{studentId}/roadmap/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkCreateRoadmap(
            Guid studentId,
            [FromBody] List<CreateStudentRoadmapRequest> roadmapItems)
        {
            try
            {
                foreach (var item in roadmapItems)
                    item.StudentId = studentId;

                var result = await _roadmapService.CreateRoadmapFromTemplateAsync(studentId, roadmapItems);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating roadmap");
                return StatusCode(500, new { message = "An error occurred while creating roadmap" });
            }
        }

        [HttpPost("api/students/{studentId}/roadmap/{roadmapId}/retake-plan")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanRetakeForStudent(Guid studentId, Guid roadmapId, [FromBody] PlanRetakeRequest request)
        {
            if (request == null || request.SemesterId == Guid.Empty)
            {
                return BadRequest(new { message = "SemesterId is required" });
            }

            try
            {
                var result = await _roadmapService.PlanRetakeAsync(studentId, roadmapId, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error planning retake for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while planning retake" });
            }
        }

        [HttpPut("api/roadmap/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoadmapEntry(Guid id, [FromBody] UpdateStudentRoadmapRequest request)
        {
            try
            {
                var result = await _roadmapService.UpdateRoadmapAsync(id, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roadmap");
                return StatusCode(500, new { message = "An error occurred while updating roadmap" });
            }
        }

        [HttpDelete("api/roadmap/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoadmapEntry(Guid id)
        {
            try
            {
                var result = await _roadmapService.DeleteRoadmapAsync(id);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting roadmap");
                return StatusCode(500, new { message = "An error occurred while deleting roadmap" });
            }
        }

        // ==================== HELPER ====================

        private Guid GetStudentIdFromToken()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;

            return Guid.TryParse(studentIdClaim, out var studentId)
                ? studentId
                : Guid.Empty;
        }
    }
}
