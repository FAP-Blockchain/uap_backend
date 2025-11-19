using Fap.Api.Interfaces;
using Fap.Domain.DTOs.StudentRoadmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    /// <summary>
    /// RESTful Student Roadmap API
    /// Following REST conventions: /students/{studentId}/roadmap
    /// </summary>
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

   /// <summary>
        /// GET /api/students/me/roadmap - Get my complete roadmap (Student)
        /// </summary>
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

        /// <summary>
        /// GET /api/students/me/roadmap/semesters/{semesterId} - Get my roadmap for specific semester (Student)
        /// </summary>
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

  /// <summary>
        /// GET /api/students/me/roadmap/current - Get my current semester roadmap (Student)
        /// </summary>
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

        /// <summary>
        /// GET /api/students/me/roadmap/recommendations - Get recommended subjects for enrollment (Student)
        /// </summary>
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

        /// <summary>
        /// GET /api/students/me/roadmap/paged - Get my paginated roadmap with filters (Student)
        /// </summary>
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

        // ==================== ADMIN APIs - CRUD OPERATIONS ====================

        /// <summary>
        /// GET /api/students/{studentId}/roadmap - Get student's complete roadmap (Admin)
     /// </summary>
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

        /// <summary>
    /// GET /api/students/{studentId}/roadmap/semesters/{semesterId} - Get student roadmap by semester (Admin)
      /// </summary>
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
    _logger.LogError(ex, "Error getting roadmap for student {StudentId} semester {SemesterId}",
     studentId, semesterId);
       return StatusCode(500, new { message = "An error occurred while retrieving roadmap" });
            }
 }

        /// <summary>
     /// GET /api/roadmap/{id} - Get roadmap entry details by ID (Admin)
  /// </summary>
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

        /// <summary>
        /// POST /api/students/{studentId}/roadmap - Create roadmap entry for student (Admin)
        /// </summary>
    [HttpPost("api/students/{studentId}/roadmap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoadmapEntry(
      Guid studentId, 
         [FromBody] CreateStudentRoadmapRequest request)
        {
            try
            {
           // Ensure studentId in route matches request
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
   _logger.LogError(ex, "Error creating roadmap for student {StudentId}", studentId);
          return StatusCode(500, new { message = "An error occurred while creating roadmap" });
            }
        }

        /// <summary>
        /// POST /api/students/{studentId}/roadmap/bulk - Bulk create roadmap from template (Admin)
        /// </summary>
        [HttpPost("api/students/{studentId}/roadmap/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkCreateRoadmap(
        Guid studentId,
[FromBody] List<CreateStudentRoadmapRequest> roadmapItems)
 {
            try
            {
     // Ensure all items have correct studentId
   foreach (var item in roadmapItems)
 {
            item.StudentId = studentId;
                }

      var result = await _roadmapService.CreateRoadmapFromTemplateAsync(studentId, roadmapItems);

                if (!result.Success)
           return BadRequest(result);

                return Ok(result);
       }
         catch (Exception ex)
     {
          _logger.LogError(ex, "Error bulk creating roadmap for student {StudentId}", studentId);
        return StatusCode(500, new { message = "An error occurred while creating roadmap" });
     }
        }

        /// <summary>
        /// PUT /api/roadmap/{id} - Update roadmap entry (Admin)
  /// </summary>
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
      _logger.LogError(ex, "Error updating roadmap {RoadmapId}", id);
      return StatusCode(500, new { message = "An error occurred while updating roadmap" });
      }
     }

  /// <summary>
        /// DELETE /api/roadmap/{id} - Delete roadmap entry (Admin)
    /// </summary>
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
       _logger.LogError(ex, "Error deleting roadmap {RoadmapId}", id);
        return StatusCode(500, new { message = "An error occurred while deleting roadmap" });
         }
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Extract StudentId from JWT token claims
        /// </summary>
        private Guid GetStudentIdFromToken()
        {
   var studentIdClaim = User.FindFirst("StudentId")?.Value;
         if (string.IsNullOrEmpty(studentIdClaim) || !Guid.TryParse(studentIdClaim, out var studentId))
            {
         return Guid.Empty;
        }
            return studentId;
        }
    }
}
