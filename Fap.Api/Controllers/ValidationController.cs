using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Validation;
using Fap.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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
                .AsNoTracking()
                .Include(c => c.Student)
                    .ThenInclude(s => s!.User)
                .Include(c => c.CertificateTemplate)
                .OrderByDescending(c => c.IssuedDate)
                .Take(50)
                .Select(c => new
                {
                    id = c.Id,
                    studentId = c.StudentId,
                    studentName = c.Student != null && c.Student.User != null ? c.Student.User.FullName : "Unknown",
                    studentCode = c.Student != null ? c.Student.StudentCode : "Unknown",
                    certificateName = c.CertificateTemplate != null ? c.CertificateTemplate.Name : "Unknown",
                    fileUrl = c.FileUrl,
                    ipfsHash = c.IPFSHash,
                    issuedDate = c.IssuedDate,
                    isOnBlockchain = c.IsOnBlockchain
                })
                .ToListAsync();

            return Ok(new { success = true, data = credentials });
        }

        [HttpGet("latest_credential")]
        public async Task<IActionResult> GetLatestCredential()
        {
            var credential = await _context.Credentials
                .AsNoTracking()
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

        [HttpGet("grades")]
        public async Task<IActionResult> GetLatestGrades()
        {
            var grades = await _context.Grades
                .AsNoTracking()
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .OrderByDescending(g => g.UpdatedAt)
                .ThenByDescending(g => g.Id)
                .Take(50)
                .Select(g => new
                {
                    id = g.Id,
                    studentId = g.StudentId,
                    studentName = g.Student != null && g.Student.User != null ? g.Student.User.FullName : "Unknown",
                    studentCode = g.Student != null ? g.Student.StudentCode : "Unknown",
                    subjectId = g.SubjectId,
                    subjectCode = g.Subject != null ? g.Subject.SubjectCode : "Unknown",
                    subjectName = g.Subject != null ? g.Subject.SubjectName : "Unknown",
                    gradeComponentId = g.GradeComponentId,
                    gradeComponentName = g.GradeComponent != null ? g.GradeComponent.Name : "Unknown",
                    score = g.Score,
                    letterGrade = g.LetterGrade,
                    updatedAt = g.UpdatedAt,
                    onChainGradeId = g.OnChainGradeId,
                    onChainTxHash = g.OnChainTxHash,
                    onChainBlockNumber = g.OnChainBlockNumber,
                    onChainChainId = g.OnChainChainId,
                    onChainContractAddress = g.OnChainContractAddress
                })
                .ToListAsync();

            return Ok(new { success = true, data = grades });
        }

        [HttpPut("tamper_grade/{id}")]
        public async Task<IActionResult> TamperGrade(Guid id, [FromBody] TamperGradeRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (request.Score < 0m || request.Score > 10m)
            {
                return BadRequest(new { success = false, message = "Score must be between 0 and 10." });
            }

            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.Id == id);
            if (grade == null)
            {
                return NotFound(new { success = false, message = "Grade not found." });
            }

            var originalScore = grade.Score;
            var originalUpdatedAt = grade.UpdatedAt;

            grade.Score = request.Score;
            grade.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Tampered with grade {Id}. Score {OldScore}->{NewScore}", grade.Id, originalScore, grade.Score);

            return Ok(new
            {
                success = true,
                message = "Grade has been tampered with. Verification should now fail if the grade is on-chain.",
                data = new
                {
                    id = grade.Id,
                    studentId = grade.StudentId,
                    subjectId = grade.SubjectId,
                    gradeComponentId = grade.GradeComponentId,
                    originalScore,
                    newScore = grade.Score,
                    originalUpdatedAt,
                    newUpdatedAt = grade.UpdatedAt,
                    onChainGradeId = grade.OnChainGradeId,
                    onChainTxHash = grade.OnChainTxHash
                }
            });
        }

        [HttpGet("attendances")]
        public async Task<IActionResult> GetLatestAttendances()
        {
            var attendances = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Subject)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.TimeSlot)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.Class)
                .OrderByDescending(a => a.RecordedAt)
                .ThenByDescending(a => a.Id)
                .Take(50)
                .Select(a => new
                {
                    id = a.Id,
                    studentId = a.StudentId,
                    studentName = a.Student != null && a.Student.User != null ? a.Student.User.FullName : "Unknown",
                    studentCode = a.Student != null ? a.Student.StudentCode : "Unknown",
                    subjectId = a.SubjectId,
                    subjectCode = a.Subject != null ? a.Subject.SubjectCode : "Unknown",
                    subjectName = a.Subject != null ? a.Subject.SubjectName : "Unknown",
                    slotId = a.SlotId,
                    classId = a.Slot != null ? a.Slot.ClassId : (Guid?)null,
                    classCode = a.Slot != null && a.Slot.Class != null ? a.Slot.Class.ClassCode : "Unknown",
                    date = a.Slot != null ? a.Slot.Date : (DateTime?)null,
                    timeSlotName = a.Slot != null && a.Slot.TimeSlot != null ? a.Slot.TimeSlot.Name : "Unknown",
                    isPresent = a.IsPresent,
                    isExcused = a.IsExcused,
                    notes = a.Notes,
                    excuseReason = a.ExcuseReason,
                    recordedAt = a.RecordedAt,
                    onChainRecordId = a.OnChainRecordId,
                    onChainTransactionHash = a.OnChainTransactionHash,
                    isOnBlockchain = a.IsOnBlockchain
                })
                .ToListAsync();

            return Ok(new { success = true, data = attendances });
        }

        [HttpPut("tamper_attendance/{id}")]
        public async Task<IActionResult> TamperAttendance(Guid id, [FromBody] TamperAttendanceRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null)
            {
                return NotFound(new { success = false, message = "Attendance not found." });
            }

            var originalIsPresent = attendance.IsPresent;
            var originalIsExcused = attendance.IsExcused;

            attendance.IsPresent = request.IsPresent;

            if (attendance.IsPresent)
            {
                attendance.IsExcused = false;
                attendance.ExcuseReason = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Tampered with attendance {Id}. Present {OldPresent}->{NewPresent}, Excused {OldExcused}->{NewExcused}",
                attendance.Id,
                originalIsPresent,
                attendance.IsPresent,
                originalIsExcused,
                attendance.IsExcused);

            return Ok(new
            {
                success = true,
                message = "Attendance has been tampered with. Verification should now fail if the record is on-chain.",
                data = new
                {
                    id = attendance.Id,
                    studentId = attendance.StudentId,
                    subjectId = attendance.SubjectId,
                    slotId = attendance.SlotId,
                    originalIsPresent,
                    newIsPresent = attendance.IsPresent,
                    originalIsExcused,
                    newIsExcused = attendance.IsExcused,
                    onChainRecordId = attendance.OnChainRecordId,
                    onChainTransactionHash = attendance.OnChainTransactionHash,
                    isOnBlockchain = attendance.IsOnBlockchain
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
            credential.FileUrl = request.FileUrl;

            if (!string.IsNullOrEmpty(request.IPFSHash))
            {
                credential.IPFSHash = request.IPFSHash;
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Tampered with credential {Id}. Original URL: {OrigUrl}, New URL: {NewUrl}", credential.Id, originalUrl, credential.FileUrl);

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

            var originalUrl = credential.FileUrl;
            var originalHash = credential.IPFSHash;

            credential.FileUrl = request.FileUrl;
            credential.IPFSHash = !string.IsNullOrEmpty(request.IPFSHash)
                ? request.IPFSHash
                : "QmTamperedHash1234567890TamperedHash1234567890";

            await _context.SaveChangesAsync();

            _logger.LogWarning("Tampered with credential {Id}. Original URL: {OrigUrl}, New URL: {NewUrl}", credential.Id, originalUrl, credential.FileUrl);

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
        public async Task<IActionResult> GetAttendanceDateValidation()
        {
            return Ok(new { success = true, data = new { enabled = await _validationService.IsAttendanceDateValidationEnabledAsync() } });
        }

        [HttpPost("attendance_date")]
        public async Task<IActionResult> SetAttendanceDateValidation([FromBody] AttendanceValidationToggleRequest request)
        {
            await _validationService.SetAttendanceDateValidationAsync(request.Enabled);
            _logger.LogInformation("Attendance date validation toggled to {Enabled}", request.Enabled);

            return Ok(new
            {
                success = true,
                message = request.Enabled ? "Attendance date validation enabled" : "Attendance date validation disabled",
                data = new { enabled = request.Enabled }
            });
        }
    }
}
