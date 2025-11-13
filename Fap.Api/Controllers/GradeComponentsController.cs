using Fap.Api.Interfaces;
using Fap.Domain.DTOs.GradeComponent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/grade-components")]
    [Authorize]
    public class GradeComponentsController : ControllerBase
    {
        private readonly IGradeComponentService _gradeComponentService;
        private readonly ILogger<GradeComponentsController> _logger;

        public GradeComponentsController(
            IGradeComponentService gradeComponentService,
            ILogger<GradeComponentsController> logger)
        {
            _gradeComponentService = gradeComponentService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/grade-components - Get all grade components
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllGradeComponents()
        {
            try
            {
                var components = await _gradeComponentService.GetAllGradeComponentsAsync();
                return Ok(components);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting grade components: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving grade components" });
            }
        }

        /// <summary>
        /// GET /api/grade-components/{id} - Get grade component by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGradeComponentById(Guid id)
        {
            try
            {
                var component = await _gradeComponentService.GetGradeComponentByIdAsync(id);

                if (component == null)
                    return NotFound(new { message = $"Grade component with ID {id} not found" });

                return Ok(component);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting grade component {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving grade component" });
            }
        }

        /// <summary>
        /// POST /api/grade-components - Create a new grade component
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGradeComponent([FromBody] CreateGradeComponentRequest request)
        {
            try
            {
                var result = await _gradeComponentService.CreateGradeComponentAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetGradeComponentById),
                    new { id = result.GradeComponentId },
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating grade component: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating grade component" });
            }
        }

        /// <summary>
        /// PUT /api/grade-components/{id} - Update a grade component
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateGradeComponent(Guid id, [FromBody] UpdateGradeComponentRequest request)
        {
            try
            {
                var result = await _gradeComponentService.UpdateGradeComponentAsync(id, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating grade component {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating grade component" });
            }
        }

        /// <summary>
        /// DELETE /api/grade-components/{id} - Delete a grade component
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGradeComponent(Guid id)
        {
            try
            {
                var result = await _gradeComponentService.DeleteGradeComponentAsync(id);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting grade component {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting grade component" });
            }
        }
    }
}
