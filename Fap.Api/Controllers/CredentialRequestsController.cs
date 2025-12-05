using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Credential;
using Fap.Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    /// <summary>
    /// Credential Requests Controller - Students request certificates, Admin approves
    /// </summary>
    [ApiController]
    [Route("api/credential-requests")]
    public class CredentialRequestsController : ControllerBase
    {
        private readonly ICredentialService _credentialService;
        private readonly ILogger<CredentialRequestsController> _logger;

        public CredentialRequestsController(
    ICredentialService credentialService,
            ILogger<CredentialRequestsController> logger)
        {
            _credentialService = credentialService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }

        /// <summary>
        /// GET /api/credential-requests - Get all credential requests (Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<CredentialRequestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CredentialRequestDto>>> GetCredentialRequests(
    [FromQuery] GetCredentialRequestsRequest request)
        {
            try
            {
                var result = await _credentialService.GetCredentialRequestsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential requests");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/credential-requests/{id} - Get credential request by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(CredentialRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CredentialRequestDto>> GetCredentialRequestById(Guid id)
        {
            try
            {
                var credentialRequest = await _credentialService.GetCredentialRequestByIdAsync(id);

                if (credentialRequest == null)
                {
                    return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
                }

                return Ok(credentialRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential request {RequestId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// POST /api/credential-requests - Create credential request (Student)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(CredentialRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CredentialRequestDto>> CreateCredentialRequest(
   [FromBody] RequestCredentialRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var credentialRequest = await _credentialService.RequestCredentialAsync(userId, request);

                return CreatedAtAction(
              nameof(GetCredentialRequestById),
             new { id = credentialRequest.Id },
                   credentialRequest
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credential request");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// POST /api/credential-requests/{id}/approve - Approve request and issue credential (Admin)
        /// Issues credential (off-chain) and returns detail + on-chain payload for frontend to call blockchain.
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CredentialDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CredentialDetailDto>> ApproveRequest(
            Guid id,
  [FromBody] ProcessCredentialRequestRequest request)
        {
            try
            {
                request.Action = "Approve";
                var userId = GetCurrentUserId();
                var credential = await _credentialService.ProcessCredentialRequestAsync(id, request, userId);

                return Ok(credential);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request",
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving credential request {RequestId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// POST /api/credential-requests/{id}/reject - Reject request (Admin)
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectRequest(
      Guid id,
         [FromBody] ProcessCredentialRequestRequest request)
        {
            try
            {
                request.Action = "Reject";
                var userId = GetCurrentUserId();
                await _credentialService.ProcessCredentialRequestAsync(id, request, userId);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting credential request {RequestId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/students/me/credential-requests - Get my requests
        /// </summary>
        [HttpGet("/api/students/me/credential-requests")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(List<CredentialRequestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CredentialRequestDto>>> GetMyCredentialRequests(
     [FromQuery] string? status = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var requests = await _credentialService.GetStudentCredentialRequestsAsync(userId, status);

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student's credential requests");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }
    }
}
