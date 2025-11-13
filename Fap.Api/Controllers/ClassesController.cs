using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Class;
using Fap.Domain.DTOs.Grade;
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
        private readonly ILogger<ClassesController> _logger;

        public ClassesController(
            IClassService classService,
            IGradeService gradeService,
            ILogger<ClassesController> logger)
        {
            _classService = classService;
            _gradeService = gradeService;
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
                _logger.LogError($"? Error getting classes: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving classes" });
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
                _logger.LogError($"? Error getting class {id}: {ex.Message}");
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
                _logger.LogError($"? Error creating class: {ex.Message}");
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
                _logger.LogError($"? Error updating class {id}: {ex.Message}");
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
                _logger.LogError($"? Error deleting class {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting class" });
            }
        }

        /// <summary>
        /// Get class roster (list of students in the class)
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
                _logger.LogError($"? Error getting class roster for {id}: {ex.Message}");
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
    }
}
