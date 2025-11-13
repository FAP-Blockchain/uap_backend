using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Grade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GradesController : ControllerBase
    {
        private readonly IGradeService _gradeService;
        private readonly ILogger<GradesController> _logger;

        public GradesController(
            IGradeService gradeService,
            ILogger<GradesController> logger)
        {
            _gradeService = gradeService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/grades - Teacher creates a grade
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> CreateGrade([FromBody] CreateGradeRequest request)
        {
            try
            {
                var result = await _gradeService.CreateGradeAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetGradeById),
                    new { id = result.GradeId },
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating grade: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating grade" });
            }
        }

        /// <summary>
        /// GET /api/grades/{id} - Get grade details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGradeById(Guid id)
        {
            try
            {
                var grade = await _gradeService.GetGradeByIdAsync(id);

                if (grade == null)
                    return NotFound(new { message = $"Grade with ID {id} not found" });

                return Ok(grade);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting grade {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving grade" });
            }
        }

        /// <summary>
        /// PUT /api/grades/{id} - Update a grade
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> UpdateGrade(Guid id, [FromBody] UpdateGradeRequest request)
        {
            try
            {
                var result = await _gradeService.UpdateGradeAsync(id, request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating grade {id}: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating grade" });
            }
        }
    }
}
