using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [Route("api/validation")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ValidationController : ControllerBase
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(IValidationService validationService, ILogger<ValidationController> logger)
        {
            _validationService = validationService;
            _logger = logger;
        }

        [HttpGet("attendance_date")]
        public IActionResult GetAttendanceDateValidation()
        {
            return Ok(new
            {
                success = true,
                data = new
                {
                    enabled = _validationService.IsAttendanceDateValidationEnabled
                }
            });
        }

        [HttpPost("attendance_date")]
        public async Task<IActionResult> SetAttendanceDateValidation([FromBody] AttendanceValidationToggleRequest request)
        {
            await _validationService.SetAttendanceDateValidationAsync(request.Enabled);
            _logger.LogInformation("Attendance date validation toggled to {Enabled}", request.Enabled);

            return Ok(new
            {
                success = true,
                message = request.Enabled
                    ? "Attendance date validation enabled"
                    : "Attendance date validation disabled",
                data = new
                {
                    enabled = request.Enabled
                }
            });
        }
    }
}
