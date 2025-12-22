using Fap.Api.Interfaces;
using Fap.Api.Extensions;
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

        // ===== ON-CHAIN (GradeManagement) =====

        /// <summary>
        /// GET /api/grades/{id}/on-chain/prepare
        /// Chuẩn bị payload để FE gọi GradeManagement.recordGrade(...)
        /// </summary>
        [HttpGet("{id}/on-chain/prepare")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(GradeOnChainPrepareDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PrepareGradeOnChain(Guid id)
        {
            try
            {
                var dto = await _gradeService.PrepareGradeOnChainAsync(id);
                if (dto == null)
                {
                    return NotFound(new { message = $"Cannot prepare on-chain payload for grade {id}" });
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing grade {GradeId} for on-chain", id);
                return StatusCode(500, new { message = "An error occurred while preparing grade for on-chain" });
            }
        }

        /// <summary>
        /// POST /api/grades/{id}/on-chain
        /// Lưu thông tin transaction on-chain sau khi FE đã gọi contract thành công
        /// </summary>
        [HttpPost("{id}/on-chain")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveGradeOnChain(Guid id, [FromBody] SaveGradeOnChainRequest request)
        {
            try
            {
                var userId = User.GetRequiredUserId();
                var result = await _gradeService.SaveGradeOnChainAsync(id, request, userId);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving on-chain info for grade {GradeId}", id);
                return StatusCode(500, new { message = "An error occurred while saving grade on-chain info" });
            }
        }

        /// <summary>
        /// GET /api/grades/verify-list?studentId=...&classId=...
        /// Verify all grade records of a student in a class by comparing DB with on-chain records.
        /// </summary>
        [HttpGet("verify-list")]
        [Authorize(Roles = "Teacher,Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyGradeList([FromQuery] Guid studentId, [FromQuery] Guid classId)
        {
            try
            {
                var data = await _gradeService.VerifyGradeListAsync(studentId, classId);
                return Ok(new { success = true, data });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying grade list for student {StudentId} class {ClassId}", studentId, classId);
                return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
            }
        }
    }
}
