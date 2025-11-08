using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;
        private readonly ILogger<SubjectsController> _logger;

        public SubjectsController(ISubjectService subjectService, ILogger<SubjectsController> logger)
        {
  _subjectService = subjectService;
_logger = logger;
    }

/// <summary>
        /// Get paginated list of subjects with filtering and sorting
    /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSubjects([FromQuery] GetSubjectsRequest request)
        {
   try
            {
 var (subjects, totalCount) = await _subjectService.GetSubjectsAsync(request);
        
             return Ok(new
       {
            data = subjects,
 totalCount,
      pageNumber = request.PageNumber,
      pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
  });
            }
            catch (Exception ex)
        {
      _logger.LogError($"? Error getting subjects: {ex.Message}");
    return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
            }
      }

      /// <summary>
        /// Get subject by ID with full details
   /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubjectById(Guid id)
        {
            try
        {
  var subject = await _subjectService.GetSubjectByIdAsync(id);
                
                if (subject == null)
   return NotFound(new { message = $"Subject with ID {id} not found" });
    
   return Ok(subject);
      }
       catch (Exception ex)
    {
       _logger.LogError($"? Error getting subject {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving subject" });
          }
        }

        /// <summary>
        /// Create a new subject
        /// </summary>
        [HttpPost]
      [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
        {
       try
    {
    if (!ModelState.IsValid)
               return BadRequest(ModelState);

     var (success, message, subjectId) = await _subjectService.CreateSubjectAsync(request);
      
           if (!success)
         return BadRequest(new { message });
            
           return CreatedAtAction(
       nameof(GetSubjectById), 
           new { id = subjectId }, 
             new { message, subjectId }
                );
      }
         catch (Exception ex)
  {
           _logger.LogError($"? Error creating subject: {ex.Message}");
      return StatusCode(500, new { message = "An error occurred while creating subject" });
          }
        }

     /// <summary>
        /// Update an existing subject
        /// </summary>
        [HttpPut("{id}")]
      [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectRequest request)
        {
            try
            {
if (!ModelState.IsValid)
         return BadRequest(ModelState);

        var (success, message) = await _subjectService.UpdateSubjectAsync(id, request);
            
      if (!success)
           return BadRequest(new { message });
           
                return Ok(new { message });
            }
            catch (Exception ex)
         {
           _logger.LogError($"? Error updating subject {id}: {ex.Message}");
           return StatusCode(500, new { message = "An error occurred while updating subject" });
    }
    }

        /// <summary>
        /// Delete a subject
        /// </summary>
[HttpDelete("{id}")]
  [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSubject(Guid id)
   {
        try
         {
         var (success, message) = await _subjectService.DeleteSubjectAsync(id);
           
         if (!success)
         return BadRequest(new { message });
            
     return Ok(new { message });
        }
         catch (Exception ex)
            {
           _logger.LogError($"? Error deleting subject {id}: {ex.Message}");
  return StatusCode(500, new { message = "An error occurred while deleting subject" });
  }
      }
    }
}
