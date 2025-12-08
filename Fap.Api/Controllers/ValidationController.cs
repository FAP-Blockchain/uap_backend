using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Validation;
using Fap.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [Route("api/validation")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ValidationController : ControllerBase
    {
        private readonly IValidationService _validationService;
        private readonly FapDbContext _context;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IValidationService validationService, 
            FapDbContext context,
            ILogger<ValidationController> logger)
        {
            _validationService = validationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("credentials")]
        public async Task<IActionResult> GetCredentials()
        {
            var credentials = await _context.Credentials
                .Include(c => c.Student).ThenInclude(s => s.User)
                .Include(c => c.CertificateTemplate)
                .OrderByDescending(c => c.IssuedDate)
                .Take(50) // Limit to last 50
                .Select(c => new
                {
                    id = c.Id,
                    studentId = c.StudentId,
                    studentName = c.Student != null && c.Student.User != null ? c.Student.User.FullName : "Unknown",
                    certificateName = c.CertificateTemplate != null ? c.CertificateTemplate.Name : c.CertificateType,
                    fileUrl = c.FileUrl,
                    ipfsHash = c.IPFSHash,
                    issuedDate = c.IssuedDate,
                    isOnBlockchain = c.IsOnBlockchain
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = credentials
            });
        }

        [HttpGet("latest_credential")]
        public async Task<IActionResult> GetLatestCredential()
        {
            var credential = await _context.Credentials
                .OrderByDescending(c => c.IssuedDate)
                .FirstOrDefaultAsync();

            if (credential == null)
            {
                return NotFound(new { success = false, message = "No credentials found." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = credential.Id,
                    studentId = credential.StudentId,
                    fileUrl = credential.FileUrl,
                    ipfsHash = credential.IPFSHash,
                    issuedDate = credential.IssuedDate,
                    isOnBlockchain = credential.IsOnBlockchain
                }
            });
        }

        [HttpPut("tamper_credential/{id}")]
        public async Task<IActionResult> TamperCredential(Guid id, [FromBody] TamperCredentialRequest request)
        {
            var credential = await _context.Credentials.FindAsync(id);

            if (credential == null)
            {
                return NotFound(new { success = false, message = "Credential not found." });
            }

            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { success = false, message = "FileUrl is required." });
            }

            var originalUrl = credential.FileUrl;
            
            // Tamper with the data
            credential.FileUrl = request.FileUrl;
            // Use provided hash or a default fake one if provided, otherwise keep original hash (mismatch)
            // Actually, to simulate tampering, we usually change the file content (FileUrl points to new file)
            // but keep the Hash the same (so verification fails because Hash(NewFile) != OldHash).
            // OR we change the Hash to something else.
            // The user request says "thay đổi file", implying changing FileUrl.
            // If we want verification to fail, we just need FileUrl to point to a file that hashes to something DIFFERENT than credential.IPFSHash.
            // So we update FileUrl. We can optionally update IPFSHash if we want to simulate "Database Tampering" of the hash too, 
            // but usually tampering means the file content changed but the blockchain record (which we can't change) remains.
            // However, the previous implementation also updated IPFSHash in DB. 
            // Let's stick to the previous logic: Update FileUrl in DB. 
            // If request.IPFSHash is provided, update it in DB too.
            
            if (!string.IsNullOrEmpty(request.IPFSHash))
            {
                credential.IPFSHash = request.IPFSHash;
            }
            
            await _context.SaveChangesAsync();

            _logger.LogWarning("Tampered with credential {Id}. Original URL: {OrigUrl}, New URL: {NewUrl}", 
                credential.Id, originalUrl, credential.FileUrl);

            return Ok(new
            {
                success = true,
                message = "Credential has been tampered with.",
                data = new
                {
                    id = credential.Id,
                    studentId = credential.StudentId,
                    fileUrl = credential.FileUrl,
                    ipfsHash = credential.IPFSHash,
                    issuedDate = credential.IssuedDate,
                    isOnBlockchain = credential.IsOnBlockchain
                }
            });
        }

        [HttpPut("tamper_credential")]
        public async Task<IActionResult> TamperLatestCredential([FromBody] TamperCredentialRequest request)
        {
            var credential = await _context.Credentials
                .OrderByDescending(c => c.IssuedDate)
                .FirstOrDefaultAsync();

            if (credential == null)
            {
                return NotFound(new { success = false, message = "No credentials found to tamper." });
            }

            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { success = false, message = "FileUrl is required." });
            }

            // Tamper with the data
            // We change the FileUrl and IPFSHash so that off-chain data no longer matches on-chain hash
            var originalUrl = credential.FileUrl;
            var originalHash = credential.IPFSHash;

            // Point to the user-provided file
            credential.FileUrl = request.FileUrl;
            // Use provided hash or a default fake one
            credential.IPFSHash = !string.IsNullOrEmpty(request.IPFSHash) 
                ? request.IPFSHash 
                : "QmTamperedHash1234567890TamperedHash1234567890"; 
            
            await _context.SaveChangesAsync();

            _logger.LogWarning("Tampered with credential {Id}. Original URL: {OrigUrl}, New URL: {NewUrl}", 
                credential.Id, originalUrl, credential.FileUrl);

            return Ok(new
            {
                success = true,
                message = "Latest credential has been tampered with. Verification should now fail.",
                data = new
                {
                    id = credential.Id,
                    studentId = credential.StudentId,
                    originalUrl,
                    originalHash,
                    newUrl = credential.FileUrl,
                    newHash = credential.IPFSHash
                }
            });
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
