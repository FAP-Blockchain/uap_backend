using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fap.Api.Controllers
{
    [ApiController]
 [Route("api/[controller]")]
    [Authorize]
    public class SubjectOfferingsController : ControllerBase
    {
     private readonly ISubjectOfferingService _service;
 private readonly ILogger<SubjectOfferingsController> _logger;

public SubjectOfferingsController(
    ISubjectOfferingService service,
            ILogger<SubjectOfferingsController> logger)
   {
       _service = service;
        _logger = logger;
        }

        /// <summary>
        /// Get paginated list of subject offerings with filtering
        /// </summary>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Paginated list of subject offerings</returns>
        [HttpGet]
      public async Task<IActionResult> GetSubjectOfferings([FromQuery] GetSubjectOfferingsRequest request)
  {
        try
            {
      var result = await _service.GetSubjectOfferingsAsync(request);
     return Ok(result);
            }
            catch (Exception ex)
     {
         _logger.LogError($"? Error getting subject offerings: {ex.Message}");
     return StatusCode(500, new { message = "An error occurred while retrieving subject offerings", error = ex.Message });
      }
        }

   /// <summary>
    /// Get subject offering by ID
        /// </summary>
        /// <param name="id">Subject offering ID</param>
        /// <returns>Subject offering details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubjectOfferingById(Guid id)
        {
         try
         {
                var offering = await _service.GetSubjectOfferingByIdAsync(id);

             if (offering == null)
    return NotFound(new { message = $"Subject offering with ID {id} not found" });

             return Ok(offering);
            }
  catch (Exception ex)
            {
              _logger.LogError($"? Error getting subject offering {id}: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while retrieving subject offering" });
       }
        }

        /// <summary>
        /// Get all subject offerings for a specific semester
        /// </summary>
  /// <param name="semesterId">Semester ID</param>
  /// <returns>List of subject offerings in the semester</returns>
        [HttpGet("semester/{semesterId}")]
        public async Task<IActionResult> GetSubjectOfferingsBySemester(Guid semesterId)
        {
 try
   {
    var offerings = await _service.GetSubjectOfferingsBySemesterAsync(semesterId);
          return Ok(offerings);
     }
            catch (Exception ex)
            {
       _logger.LogError($"? Error getting subject offerings for semester {semesterId}: {ex.Message}");
      return StatusCode(500, new { message = "An error occurred while retrieving subject offerings" });
    }
        }

        /// <summary>
        /// Get all offerings for a specific subject across all semesters
        /// </summary>
        /// <param name="subjectId">Subject ID</param>
     /// <returns>List of subject offerings</returns>
        [HttpGet("subject/{subjectId}")]
        public async Task<IActionResult> GetSubjectOfferingsBySubject(Guid subjectId)
        {
        try
  {
           var offerings = await _service.GetSubjectOfferingsBySubjectAsync(subjectId);
            return Ok(offerings);
 }
      catch (Exception ex)
       {
          _logger.LogError($"? Error getting subject offerings for subject {subjectId}: {ex.Message}");
       return StatusCode(500, new { message = "An error occurred while retrieving subject offerings" });
       }
        }
    }
}
