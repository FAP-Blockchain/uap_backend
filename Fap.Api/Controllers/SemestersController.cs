using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Semester;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SemestersController : ControllerBase
    {
        private readonly ISemesterService _semesterService;
        private readonly ILogger<SemestersController> _logger;

        public SemestersController(ISemesterService semesterService, ILogger<SemestersController> logger)
 {
            _semesterService = semesterService;
      _logger = logger;
        }

     /// <summary>
     /// Get paginated list of semesters with filtering and sorting
 /// </summary>
[HttpGet]
        public async Task<IActionResult> GetSemesters([FromQuery] GetSemestersRequest request)
        {
            try
            {
       var (semesters, totalCount) = await _semesterService.GetSemestersAsync(request);
      
       return Ok(new
        {
       data = semesters,
         totalCount,
                    pageNumber = request.PageNumber,
            pageSize = request.PageSize,
        totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
             });
         }
   catch (Exception ex)
     {
 _logger.LogError($"? Error getting semesters: {ex.Message}");
        return StatusCode(500, new { message = "An error occurred while retrieving semesters" });
    }
      }

        /// <summary>
      /// Get semester by ID with full details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSemesterById(Guid id)
        {
        try
       {
var semester = await _semesterService.GetSemesterByIdAsync(id);
    
         if (semester == null)
          return NotFound(new { message = $"Semester with ID {id} not found" });
             
            return Ok(semester);
   }
            catch (Exception ex)
            {
      _logger.LogError($"? Error getting semester {id}: {ex.Message}");
        return StatusCode(500, new { message = "An error occurred while retrieving semester" });
  }
 }

   /// <summary>
    /// Create a new semester
      /// </summary>
    [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request)
        {
            try
    {
 if (!ModelState.IsValid)
   return BadRequest(ModelState);

    var (success, message, semesterId) = await _semesterService.CreateSemesterAsync(request);
       
     if (!success)
    return BadRequest(new { message });
                
        return CreatedAtAction(
          nameof(GetSemesterById), 
                  new { id = semesterId }, 
      new { message, semesterId }
   );
            }
        catch (Exception ex)
     {
  _logger.LogError($"? Error creating semester: {ex.Message}");
   return StatusCode(500, new { message = "An error occurred while creating semester" });
   }
  }

        /// <summary>
     /// Update an existing semester
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSemester(Guid id, [FromBody] UpdateSemesterRequest request)
        {
     try
            {
 if (!ModelState.IsValid)
            return BadRequest(ModelState);

       var (success, message) = await _semesterService.UpdateSemesterAsync(id, request);
          
            if (!success)
  return BadRequest(new { message });
          
     return Ok(new { message });
     }
            catch (Exception ex)
            {
          _logger.LogError($"? Error updating semester {id}: {ex.Message}");
    return StatusCode(500, new { message = "An error occurred while updating semester" });
            }
        }

        /// <summary>
        /// Close a semester (prevent further modifications)
        /// </summary>
        [HttpPatch("{id}/close")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CloseSemester(Guid id)
        {
   try
{
            var (success, message) = await _semesterService.CloseSemesterAsync(id);
    
     if (!success)
    return BadRequest(new { message });
 
    return Ok(new { message });
 }
       catch (Exception ex)
            {
      _logger.LogError($"? Error closing semester {id}: {ex.Message}");
          return StatusCode(500, new { message = "An error occurred while closing semester" });
     }
        }
    }
}
