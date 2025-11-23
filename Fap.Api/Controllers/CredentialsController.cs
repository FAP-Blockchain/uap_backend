using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Credential;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs; // For ServiceResult
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fap.Api.Controllers
{
    /// <summary>
    /// RESTful API for Credential/Certificate Management
    /// </summary>
    [ApiController]
    [Route("api/credentials")]
    public class CredentialsController : ControllerBase
    {
        private readonly ICredentialService _credentialService;
        private readonly ILogger<CredentialsController> _logger;

        public CredentialsController(
         ICredentialService credentialService,
    ILogger<CredentialsController> logger)
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

        // ==================== CREDENTIALS RESOURCE ====================

        /// <summary>
        /// GET /api/credentials - Get all credentials (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<CredentialDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CredentialDto>>> GetCredentials(
                [FromQuery] GetCredentialsRequest request)
        {
            try
            {
                var result = await _credentialService.GetCredentialsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials");
                return StatusCode(500, new ProblemDetails
                {
                    Status = 500,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving credentials"
                });
            }
        }

        /// <summary>
        /// GET /api/credentials/{id} - Get credential by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CredentialDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CredentialDetailDto>> GetCredentialById(Guid id)
        {
            try
            {
                var credential = await _credentialService.GetCredentialByIdAsync(id);

                if (credential == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Status = 404,
                        Title = "Not Found",
                        Detail = $"Credential with ID '{id}' not found"
                    });
                }

                return Ok(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// POST /api/credentials/issue - Issue new credential (Admin only)
        /// </summary>
        [HttpPost("issue")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CredentialDetailDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IssueCredential([FromBody] IssueCredentialDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            var result = await _credentialService.IssueCredentialAsync(request);
            if (!result.Success) return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = result.Message });
            
            return Ok(result.Data);
        }

        /// <summary>
        /// POST /api/credentials - Create new credential (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CredentialDetailDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CredentialDetailDto>> CreateCredential(
              [FromBody] CreateCredentialRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var credential = await _credentialService.CreateCredentialAsync(request, userId);

                return CreatedAtAction(
            nameof(GetCredentialById),
               new { id = credential.Id },
              credential
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
                _logger.LogError(ex, "Error creating credential");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// DELETE /api/credentials/{id} - Revoke credential (Admin only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeCredential(Guid id, [FromBody] RevokeCredentialRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _credentialService.RevokeCredentialAsync(id, request, userId);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        // ==================== SUB-RESOURCES ====================

        /// <summary>
        /// GET /api/credentials/{id}/pdf - Download credential PDF
        /// </summary>
        [HttpGet("{id:guid}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadCredentialPdf(Guid id)
        {
            try
            {
                var (fileBytes, fileName) = await _credentialService.GenerateCredentialPdfAsync(id);
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading credential PDF {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/credentials/{id}/qrcode - Get QR code for credential
        /// </summary>
        [HttpGet("{id:guid}/qrcode")]
        [ProducesResponseType(typeof(QRCodeResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<QRCodeResponse>> GetCredentialQRCode(
                  Guid id,
        [FromQuery] int size = 300)
        {
            try
            {
                Guid? userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;

                var qrCodeData = await _credentialService.GenerateQRCodeAsync(id, userId, size);

                return Ok(new QRCodeResponse
                {
                    CredentialId = id,
                    QRCodeData = qrCodeData,
                    Size = size
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/credentials/{id}/share - Get shareable link
        /// </summary>
        [HttpGet("{id:guid}/share")]
        [ProducesResponseType(typeof(CredentialShareDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CredentialShareDto>> GetCredentialShareInfo(Guid id)
        {
            try
            {
                Guid? userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : null;
                var shareInfo = await _credentialService.GetCredentialShareInfoAsync(id, userId);
                return Ok(shareInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ProblemDetails { Status = 401, Title = "Unauthorized", Detail = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting share info for credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/credentials/public/{id} - Public certificate view (No authentication required)
        /// Endpoint này dành cho người xem chứng chỉ qua QR Code hoặc link chia sẻ
        /// </summary>
        [HttpGet("public/{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CertificatePublicDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CertificatePublicDto>> GetPublicCertificate(Guid id)
        {
            try
            {
                var certificate = await _credentialService.GetPublicCertificateAsync(id);
                
                if (certificate == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Status = 404,
                        Title = "Certificate Not Found",
                        Detail = "The requested certificate does not exist or has been removed."
                    });
                }

                return Ok(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public certificate {CredentialId}", id);
                return StatusCode(500, new ProblemDetails 
                { 
                    Status = 500, 
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the certificate."
                });
            }
        }

        /// <summary>
        /// POST /api/credentials/{id}/approve - Approve credential (Admin only)
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CredentialDetailDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CredentialDetailDto>> ApproveCredential(
            Guid id,
     [FromBody] ReviewCredentialRequest request)
        {
            try
            {
                request.Action = "Approve";
                var userId = GetCurrentUserId();
                var credential = await _credentialService.ReviewCredentialAsync(id, request, userId);
                return Ok(credential);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// POST /api/credentials/{id}/reject - Reject credential (Admin only)
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CredentialDetailDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CredentialDetailDto>> RejectCredential(
        Guid id,
            [FromBody] ReviewCredentialRequest request)
        {
            try
            {
                request.Action = "Reject";
                var userId = GetCurrentUserId();
                var credential = await _credentialService.ReviewCredentialAsync(id, request, userId);
                return Ok(credential);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting credential {CredentialId}", id);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        // ==================== VERIFICATION (PUBLIC) ====================

        /// <summary>
        /// POST /api/credentials/verify - Verify credential authenticity (Public)
        /// </summary>
        [HttpPost("verify")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CredentialVerificationDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CredentialVerificationDto>> VerifyCredential(
            [FromBody] VerifyCredentialRequest request)
        {
            try
            {
                var result = await _credentialService.VerifyCredentialAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying credential");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        // ==================== STUDENT CREDENTIALS ====================

        /// <summary>
        /// GET /api/students/{studentId}/credentials - Get student's credentials
        /// </summary>
        [HttpGet("/api/students/{studentId:guid}/credentials")]
        [Authorize]
        [ProducesResponseType(typeof(List<CredentialDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CredentialDto>>> GetStudentCredentials(
            Guid studentId,
             [FromQuery] string? certificateType = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var currentStudentId = await _credentialService.GetStudentIdByUserIdAsync(userId);

                if (!User.IsInRole("Admin") && currentStudentId != studentId)
                {
                    return Forbid();
                }

                var credentials = await _credentialService.GetStudentCredentialsByStudentIdAsync(
                studentId,
              certificateType);

                return Ok(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials for student {StudentId}", studentId);
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/students/me/credentials - Get current student's credentials
        /// </summary>
        [HttpGet("/api/students/me/credentials")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(List<CredentialDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CredentialDto>>> GetMyCredentials(
            [FromQuery] string? certificateType = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var credentials = await _credentialService.GetStudentCredentialsAsync(userId, certificateType);
                return Ok(credentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current student's credentials");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/students/me/credentials/summary - Get credential statistics
        /// </summary>
        [HttpGet("/api/students/me/credentials/summary")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(StudentCredentialSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<StudentCredentialSummaryDto>> GetMyCredentialSummary()
        {
            try
            {
                var userId = GetCurrentUserId();
                var summary = await _credentialService.GetStudentCredentialSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential summary");
                return StatusCode(500, new ProblemDetails { Status = 500, Title = "Internal Server Error" });
            }
        }

        /// <summary>
        /// GET /api/students/me/certificates/share - Get my certificates for sharing (with QR + URL)
        /// Endpoint này trả về danh sách chứng chỉ kèm QR Code và URL để sinh viên chia sẻ
        /// </summary>
        [HttpGet("/api/students/me/certificates/share")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(List<CertificatePublicDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CertificatePublicDto>>> GetMyCertificatesForSharing()
        {
            try
            {
                var userId = GetCurrentUserId();
                var certificates = await _credentialService.GetMyCertificatesForSharingAsync(userId);
                return Ok(certificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shareable certificates for current student");
                return StatusCode(500, new ProblemDetails 
                { 
                    Status = 500, 
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving your certificates."
                });
            }
        }
    }
}
