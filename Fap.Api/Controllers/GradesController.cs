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
        /// POST /api/grades - Teacher creates multiple grades at once
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateGrades([FromBody] BulkCreateGradesRequest request)
        {
            try
            {
                var result = await _gradeService.CreateGradesAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetAllGrades),
                    null,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk grades");
                return StatusCode(500, new { message = "An error occurred while creating grades" });
            }
        }

        /// <summary>
        /// GET /api/grades/{id} - Get grade details
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
                _logger.LogError(ex, "Error getting grade {GradeId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving grade" });
            }
        }

        /// <summary>
        /// PUT /api/grades/{id} - Update a grade
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
                _logger.LogError(ex, "Error updating grade {GradeId}", id);
                return StatusCode(500, new { message = "An error occurred while updating grade" });
            }
        }

        /// <summary>
        /// PUT /api/grades - Update multiple grades at once
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateGrades([FromBody] BulkUpdateGradesRequest request)
        {
            try
            {
                var result = await _gradeService.UpdateGradesAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bulk grades");
                return StatusCode(500, new { message = "An error occurred while updating grades" });
            }
        }

        /// <summary>
        /// GET /api/grades - Get all grades with filters
        /// Supports filtering by studentId, classId, subjectId, gradeComponentId
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<IActionResult> GetAllGrades([FromQuery] GetGradesRequest request)
        {
            try
            {
                var result = await _gradeService.GetAllGradesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting grades: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving grades" });
            }
        }
    }
}
