using Fap.Api.Interfaces;
using Fap.Domain.Constants;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs; // ServiceResult
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Fap.Infrastructure.Data;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fap.Api.Services
{
    public partial class AttendanceService : IAttendanceService
    {
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IBlockchainService _blockchainService;
    private readonly FapDbContext _db;
    private readonly ILogger<AttendanceService> _logger;
    private readonly BlockchainSettings _blockchainSettings;

        public AttendanceService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidationService validationService,
            IBlockchainService blockchainService,
            FapDbContext db,
            ILogger<AttendanceService> logger,
            IOptions<BlockchainSettings> blockchainSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validationService = validationService;
            _blockchainService = blockchainService;
            _db = db;
            _logger = logger;
            _blockchainSettings = blockchainSettings.Value;
        }

        #region Basic CRUD

        public async Task<AttendanceDto?> GetAttendanceByIdAsync(Guid id)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
            if (attendance == null) return null;

            var detailedAttendance = await _unitOfWork.Attendances.GetByStudentAndSlotAsync(
                attendance.StudentId, 
                attendance.SlotId
            );

            return _mapper.Map<AttendanceDto>(detailedAttendance);
        }

        public async Task<AttendanceDetailDto?> GetAttendanceDetailByIdAsync(Guid attendanceId)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(attendanceId);
            if (attendance == null)
            {
                return null;
            }

            var detailedAttendance = await _unitOfWork.Attendances.GetByStudentAndSlotAsync(
                attendance.StudentId,
                attendance.SlotId
            );

            if (detailedAttendance == null)
            {
                return null;
            }

            var dto = _mapper.Map<AttendanceDetailDto>(detailedAttendance);

            // Build on-chain payload for frontend to call smart contract
            var student = detailedAttendance.Student ?? (await _unitOfWork.Students
                .FindAsync(s => s.Id == detailedAttendance.StudentId)).FirstOrDefault();

            var wallet = student?.User?.WalletAddress;
            if (!string.IsNullOrWhiteSpace(wallet))
            {
                var metadata = new
                {
                    attendanceId = detailedAttendance.Id,
                    studentId = detailedAttendance.StudentId,
                    subjectId = detailedAttendance.SubjectId,
                    slotId = detailedAttendance.SlotId,
                    isPresent = detailedAttendance.IsPresent,
                    isExcused = detailedAttendance.IsExcused,
                    notes = detailedAttendance.Notes,
                    recordedAt = detailedAttendance.RecordedAt
                };

                var json = System.Text.Json.JsonSerializer.Serialize(metadata);

                dto.OnChainPayload = new AttendanceOnChainPayloadDto
                {
                    StudentWalletAddress = wallet,
                    AttendanceId = detailedAttendance.Id,
                    AttendanceDataJson = json,
                    ExpiresAtUnix = 0
                };
            }

            return dto;
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendancesBySlotIdAsync(Guid slotId)
        {
            var attendances = await _unitOfWork.Attendances.GetBySlotIdAsync(slotId);
            return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByClassIdAsync(Guid classId)
        {
            var attendances = await _unitOfWork.Attendances.GetByClassIdAsync(classId);
            return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByStudentIdAsync(Guid studentId)
        {
            var attendances = await _unitOfWork.Attendances.GetByStudentIdAsync(studentId);
            return _mapper.Map<IEnumerable<AttendanceDto>>(attendances);
        }

        #endregion

        #region Attendance Actions

#pragma warning disable CS0618 // Type or member is obsolete
        public async Task<IEnumerable<AttendanceDto>> TakeAttendanceAsync(TakeAttendanceRequest request)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(request.SlotId);

            if (slot == null)
                throw new InvalidOperationException("Slot not found");

            var classData = await _unitOfWork.Classes.GetByIdAsync(slot.ClassId);
            if (classData == null)
                throw new InvalidOperationException("Class not found");

            if (await _unitOfWork.Attendances.HasAttendanceForSlotAsync(request.SlotId))
                throw new InvalidOperationException("Attendance has already been taken for this slot");

            if (slot.Status != "Scheduled" && slot.Status != "Completed")
                throw new InvalidOperationException($"Cannot take attendance for a slot with status: {slot.Status}");

            await EnsureAttendanceDateComplianceAsync(slot.Date);

            var attendances = new List<Attendance>();

            foreach (var studentAttendance in request.Students)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(studentAttendance.StudentId);
                if (student == null)
                    throw new InvalidOperationException($"Student with ID {studentAttendance.StudentId} not found");

                var isMember = await _unitOfWork.Classes.FindAsync(c => 
                    c.Id == slot.ClassId && 
                    c.Members.Any(m => m.StudentId == studentAttendance.StudentId)
                );

                if (!isMember.Any())
                    throw new InvalidOperationException($"Student {student.StudentCode} is not a member of class {classData.ClassCode}");

                var attendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    SlotId = request.SlotId,
                    StudentId = studentAttendance.StudentId,
                    SubjectId = classData.SubjectOffering.SubjectId,
                    IsPresent = studentAttendance.IsPresent,
                    Notes = studentAttendance.Notes,
                    IsExcused = false,
                    RecordedAt = DateTime.UtcNow
                };

                await _unitOfWork.Attendances.AddAsync(attendance);
                attendances.Add(attendance);
            }

            // Chỉ lưu attendance vào DB, không gọi blockchain tại backend nữa
            await _unitOfWork.SaveChangesAsync();

            var attendancesWithDetails = await _unitOfWork.Attendances.GetBySlotIdAsync(request.SlotId);
            return _mapper.Map<IEnumerable<AttendanceDto>>(attendancesWithDetails);
        }

        public async Task<AttendanceDto?> UpdateAttendanceAsync(Guid id, UpdateAttendanceRequest request)
        {
            var attendance = (await _unitOfWork.Attendances.FindAsync(a => a.Id == id)).FirstOrDefault();

            if (attendance == null) return null;

            if (request.IsPresent && !attendance.IsPresent)
{
                attendance.IsExcused = false;
    attendance.ExcuseReason = null;
    }

            attendance.IsPresent = request.IsPresent;
            attendance.Notes = request.Notes;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            return await GetAttendanceByIdAsync(id);
        }

        public async Task<AttendanceDto?> ExcuseAbsenceAsync(Guid id, ExcuseAbsenceRequest request)
        {
            var attendance = (await _unitOfWork.Attendances.FindAsync(a => a.Id == id)).FirstOrDefault();

            if (attendance == null) return null;

            if (attendance.IsPresent)
                throw new InvalidOperationException("Cannot excuse a student who was present");

            if (attendance.IsExcused)
                throw new InvalidOperationException("This absence has already been excused");

            attendance.IsExcused = true;
            attendance.ExcuseReason = request.Reason;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            return await GetAttendanceByIdAsync(id);
        }

        #endregion

        #region Statistics & Reports

        public async Task<AttendanceStatisticsDto> GetStudentAttendanceStatisticsAsync(Guid studentId, Guid? classId = null)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException($"Student with ID {studentId} not found");
            }

            IEnumerable<Attendance> attendances;
            if (classId.HasValue)
            {
                attendances = (await _unitOfWork.Attendances.GetByStudentIdAsync(studentId))
                    .Where(a => a.Slot.ClassId == classId.Value);
            }
            else
            {
                attendances = await _unitOfWork.Attendances.GetByStudentIdAsync(studentId);
            }

            var attendanceList = attendances.ToList();
            var totalSlots = attendanceList.Count;
            var presentCount = attendanceList.Count(a => a.IsPresent);
            var absentCount = attendanceList.Count(a => !a.IsPresent);
            var excusedCount = attendanceList.Count(a => !a.IsPresent && a.IsExcused);

            var attendanceRate = totalSlots > 0
                ? Math.Round((decimal)presentCount / totalSlots * 100, 2)
                : 0;

            return new AttendanceStatisticsDto
            {
                StudentId = studentId,
                StudentCode = student.StudentCode,
                StudentName = student.User.FullName,
                TotalSlots = totalSlots,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                ExcusedCount = excusedCount,
                AttendanceRate = attendanceRate,
                AttendanceRecords = _mapper.Map<List<AttendanceDto>>(attendanceList)
            };
        }

        public async Task<ClassAttendanceReportDto> GetClassAttendanceReportAsync(Guid classId)
        {
            var classEntity = await _unitOfWork.Classes.GetByIdWithDetailsAsync(classId);
            if (classEntity == null)
            {
                throw new InvalidOperationException($"Class with ID {classId} not found");
            }

            var attendances = await _unitOfWork.Attendances.GetByClassIdAsync(classId);
            var attendanceList = attendances.ToList();

            // Get all students in class
            var students = classEntity.Members.Select(m => m.Student).ToList();
            var totalSlots = classEntity.Slots.Count;

            var studentSummaries = new List<StudentAttendanceSummary>();
            decimal totalAttendanceRate = 0;

            foreach (var student in students)
            {
                var studentAttendances = attendanceList.Where(a => a.StudentId == student.Id).ToList();
                var presentCount = studentAttendances.Count(a => a.IsPresent);
                var absentCount = studentAttendances.Count(a => !a.IsPresent);
                var excusedCount = studentAttendances.Count(a => !a.IsPresent && a.IsExcused);

                var attendanceRate = studentAttendances.Count > 0
                    ? Math.Round((decimal)presentCount / studentAttendances.Count * 100, 2)
                    : 0;

                totalAttendanceRate += attendanceRate;

                studentSummaries.Add(new StudentAttendanceSummary
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    StudentName = student.User.FullName,
                    ProfileImageUrl = student.User.ProfileImageUrl,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    ExcusedCount = excusedCount,
                    AttendanceRate = attendanceRate
                });
            }

            var averageAttendanceRate = students.Count > 0
                ? Math.Round(totalAttendanceRate / students.Count, 2)
                : 0;

            return new ClassAttendanceReportDto
            {
                ClassId = classId,
                ClassCode = classEntity.ClassCode,
                SubjectName = classEntity.SubjectOffering.Subject.SubjectName,
                TeacherName = classEntity.Teacher.User.FullName,
                TotalSlots = totalSlots,
                TotalStudents = students.Count,
                AverageAttendanceRate = averageAttendanceRate,
                StudentSummaries = studentSummaries.OrderByDescending(s => s.AttendanceRate).ToList()
            };
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendancesByFilterAsync(AttendanceFilterRequest filter)
        {
            IEnumerable<Attendance> attendances;

            // Start with base query
            if (filter.ClassId.HasValue)
            {
                attendances = await _unitOfWork.Attendances.GetByClassIdAsync(filter.ClassId.Value);
            }
            else if (filter.StudentId.HasValue)
            {
                attendances = await _unitOfWork.Attendances.GetByStudentIdAsync(filter.StudentId.Value);
            }
            else if (filter.SubjectId.HasValue)
            {
                attendances = await _unitOfWork.Attendances.GetBySubjectIdAsync(filter.SubjectId.Value);
            }
            else if (filter.FromDate.HasValue && filter.ToDate.HasValue)
            {
                attendances = await _unitOfWork.Attendances.GetByDateRangeAsync(filter.FromDate.Value, filter.ToDate.Value);
            }
            else
            {
                attendances = await _unitOfWork.Attendances.GetAllAsync();
            }

            // Apply additional filters
            var query = attendances.AsQueryable();

            if (filter.IsPresent.HasValue)
            {
                query = query.Where(a => a.IsPresent == filter.IsPresent.Value);
            }

            if (filter.IsExcused.HasValue)
            {
                query = query.Where(a => a.IsExcused == filter.IsExcused.Value);
            }

            if (filter.FromDate.HasValue && !filter.ToDate.HasValue)
            {
                query = query.Where(a => a.Slot.Date >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue && !filter.FromDate.HasValue)
            {
                query = query.Where(a => a.Slot.Date <= filter.ToDate.Value);
            }

            // Apply paging
            var pagedResults = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return _mapper.Map<IEnumerable<AttendanceDto>>(pagedResults);
        }

        private async Task EnsureAttendanceDateComplianceAsync(DateTime slotDate)
        {
            if (await _validationService.IsAttendanceDateValidationEnabledAsync())
            {
                var todayUtc = DateTime.UtcNow.Date;
                var targetDate = slotDate.Date;

                // Allow a 1-day buffer to handle timezone differences (e.g., UTC vs UTC+7)
                // This ensures that if it's "Today" in the user's timezone, it passes validation
                // even if UTC date is yesterday or tomorrow.
                var dayDifference = Math.Abs((targetDate - todayUtc).TotalDays);

                if (dayDifference > 1)
                {
                    throw new InvalidOperationException(
                        $"Attendance can only be taken on {targetDate:yyyy-MM-dd}. Current UTC date: {todayUtc:yyyy-MM-dd}. Toggle validation via /api/validation/attendance_date.");
                }
            }
        }

        #endregion

        #region Validation

        public async Task<bool> CanTakeAttendanceAsync(Guid slotId, Guid teacherUserId)
        {
            // Get slot directly using the Slots repository
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);

            if (slot == null) return false;

            // Check if teacher is assigned to this class (or is the substitute teacher)
            return slot.Class.TeacherUserId == teacherUserId ||
                (slot.SubstituteTeacherId.HasValue && slot.SubstituteTeacherId.Value == teacherUserId);
        }

        public async Task<bool> CanExcuseAbsenceAsync(Guid attendanceId, Guid studentUserId)
        {
            var attendances = await _unitOfWork.Attendances.FindAsync(a => a.Id == attendanceId);
            var attendance = attendances.FirstOrDefault();

            if (attendance == null) return false;

            // Student can only excuse their own attendance
            var student = await _unitOfWork.Students.GetByIdAsync(attendance.StudentId);
            return student != null && student.UserId == studentUserId;
        }

        public async Task<ServiceResult<bool>> SaveAttendanceOnChainAsync(Guid attendanceId, SaveAttendanceOnChainRequest request, Guid performedByUserId)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(attendanceId);
            if (attendance == null)
            {
                return ServiceResult<bool>.Fail("Attendance not found");
            }

            if (string.IsNullOrWhiteSpace(request.TransactionHash))
            {
                return ServiceResult<bool>.Fail("TransactionHash is required");
            }

            var txHash = request.TransactionHash.Trim();

            // 1) Fetch receipt from chain (do not trust FE for block/time)
            Nethereum.RPC.Eth.DTOs.TransactionReceipt receipt;
            try
            {
                receipt = await _blockchainService.WaitForTransactionReceiptAsync(txHash, timeoutSeconds: 120);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch transaction receipt for TxHash={TxHash}", txHash);
                return ServiceResult<bool>.Fail("Could not fetch transaction receipt (timeout or invalid txHash)");
            }

            var blockNumber = receipt.BlockNumber?.Value;
            if (blockNumber == null)
            {
                return ServiceResult<bool>.Fail("Receipt did not include BlockNumber");
            }

            // 2) Decode known events for audit/validation
            IReadOnlyList<(string EventName, string ContractAddress, string DetailJson)> decodedEvents;
            try
            {
                decodedEvents = await _blockchainService.DecodeReceiptEventsAsync(txHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decode receipt events for TxHash={TxHash}", txHash);
                decodedEvents = Array.Empty<(string, string, string)>();
            }

            // Only log/validate attendance-related events (avoid logging unrelated known events)
            var attendanceEvents = decodedEvents
                .Where(e =>
                    string.Equals(e.EventName, "AttendanceMarked", StringComparison.Ordinal) ||
                    string.Equals(e.EventName, "AttendanceUpdated", StringComparison.Ordinal))
                .ToList();

            if (attendanceEvents.Count == 0)
            {
                return ServiceResult<bool>.Fail("Transaction receipt did not contain an expected attendance event (AttendanceMarked/AttendanceUpdated)");
            }

            // 2.5) Resolve on-chain recordId from receipt (do not trust FE)
            long? resolvedOnChainRecordId = null;
            long? decodedClassId = null;
            string? decodedStudentAddress = null;
            foreach (var e in attendanceEvents)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(e.DetailJson);
                    var root = doc.RootElement;
                    if (root.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                    if (!root.TryGetProperty("indexedArgs", out var ia) || ia.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                    if (resolvedOnChainRecordId == null && ia.TryGetProperty("recordId", out var rid))
                    {
                        if (rid.ValueKind == System.Text.Json.JsonValueKind.String && long.TryParse(rid.GetString(), out var ridLong))
                        {
                            resolvedOnChainRecordId = ridLong;
                        }
                        else if (rid.ValueKind == System.Text.Json.JsonValueKind.Number && rid.TryGetInt64(out var ridLong2))
                        {
                            resolvedOnChainRecordId = ridLong2;
                        }
                    }

                    if (decodedClassId == null && ia.TryGetProperty("classId", out var cid))
                    {
                        if (cid.ValueKind == System.Text.Json.JsonValueKind.String && long.TryParse(cid.GetString(), out var cidLong))
                        {
                            decodedClassId = cidLong;
                        }
                        else if (cid.ValueKind == System.Text.Json.JsonValueKind.Number && cid.TryGetInt64(out var cidLong2))
                        {
                            decodedClassId = cidLong2;
                        }
                    }

                    if (decodedStudentAddress == null && ia.TryGetProperty("studentAddress", out var sa))
                    {
                        if (sa.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            decodedStudentAddress = sa.GetString();
                        }
                    }
                }
                catch
                {
                    // ignore parsing errors; we will fall back to FE value if needed
                }
            }

            if (request.OnChainRecordId > 0)
            {
                if (resolvedOnChainRecordId.HasValue && resolvedOnChainRecordId.Value > 0 && resolvedOnChainRecordId.Value != request.OnChainRecordId)
                {
                    return ServiceResult<bool>.Fail("OnChainRecordId does not match decoded recordId from receipt");
                }
                resolvedOnChainRecordId = request.OnChainRecordId;
            }

            if (!resolvedOnChainRecordId.HasValue || resolvedOnChainRecordId.Value <= 0)
            {
                return ServiceResult<bool>.Fail("Could not resolve OnChainRecordId from transaction receipt");
            }

            // 3) Fetch tx for TxFrom/TxTo (auditability) + optional contract target validation
            string? txFrom = null;
            string? txTo = null;
            try
            {
                var web3 = _blockchainService.GetWeb3();
                var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                txFrom = tx?.From;
                txTo = tx?.To;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve tx from/to for TxHash={TxHash}", txHash);
            }

            var expectedContractAddress = _blockchainSettings.Contracts?.AttendanceManagement;
            if (!string.IsNullOrWhiteSpace(expectedContractAddress) &&
                !string.IsNullOrWhiteSpace(txTo) &&
                !string.Equals(txTo, expectedContractAddress, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Fail("Transaction did not target AttendanceManagement contract");
            }

            // 3.5) Resolve OnChainClassId (server-side) for richer audit details
            long? onChainClassId = null;
            try
            {
                var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(attendance.SlotId);
                onChainClassId = slot?.Class?.OnChainClassId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve OnChainClassId for AttendanceId={AttendanceId}", attendanceId);
            }

            // Optional sanity-check: ensure decoded classId matches the DB-mapped on-chain class
            if (onChainClassId.HasValue && decodedClassId.HasValue && decodedClassId.Value > 0 && decodedClassId.Value != onChainClassId.Value)
            {
                return ServiceResult<bool>.Fail("Transaction receipt classId does not match Class.OnChainClassId");
            }

            // Optional sanity-check: ensure decoded studentAddress matches the student's wallet
            if (!string.IsNullOrWhiteSpace(decodedStudentAddress))
            {
                try
                {
                    var student = await _db.Students
                        .AsNoTracking()
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == attendance.StudentId);

                    var wallet = student?.User?.WalletAddress;
                    if (!string.IsNullOrWhiteSpace(wallet) &&
                        !string.Equals(wallet, decodedStudentAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        return ServiceResult<bool>.Fail("Transaction receipt studentAddress does not match student's wallet");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not validate student wallet against receipt for AttendanceId={AttendanceId}", attendanceId);
                }
            }

            attendance.OnChainRecordId = resolvedOnChainRecordId.Value;
            attendance.OnChainTransactionHash = txHash;
            attendance.IsOnBlockchain = true;

            _unitOfWork.Attendances.Update(attendance);

            // 4) Persist ActionLogs for audit trail (actor is whoever performed the sync)
            foreach (var e in attendanceEvents)
            {
                string detail;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(e.DetailJson);
                    var root = doc.RootElement;
                    object? indexedArgs = null;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("indexedArgs", out var ia))
                    {
                        indexedArgs = ia.Clone();
                    }

                    detail = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        attendanceId,
                        slotId = attendance.SlotId,
                        studentId = attendance.StudentId,
                        onChainClassId,
                        onChainRecordId = resolvedOnChainRecordId.Value,
                        contractAddress = e.ContractAddress,
                        eventName = e.EventName,
                        indexedArgs
                    });
                }
                catch
                {
                    detail = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        attendanceId,
                        slotId = attendance.SlotId,
                        studentId = attendance.StudentId,
                        onChainClassId,
                        onChainRecordId = resolvedOnChainRecordId.Value,
                        contractAddress = e.ContractAddress,
                        eventName = e.EventName,
                        decoded = e.DetailJson
                    });
                }

                if (detail.Length > 500)
                {
                    detail = detail.Substring(0, 500);
                }

                _db.ActionLogs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    Action = ActionLogActions.AttendanceOnChainSync,
                    Detail = detail,
                    UserId = performedByUserId,
                    TransactionHash = txHash,
                    BlockNumber = (long)blockNumber,
                    EventName = e.EventName,
                    TxFrom = txFrom,
                    TxTo = txTo,
                    ContractAddress = e.ContractAddress,
                    CredentialId = null
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResponse(true);
        }

        private static byte MapAttendanceToChainStatus(Attendance attendance)
        {
            // DataTypes.AttendanceStatus enum in solidity:
            // PRESENT=0, ABSENT=1, LATE=2, EXCUSED=3
            if (attendance.IsPresent) return 0;
            if (!attendance.IsPresent && attendance.IsExcused) return 3;
            return 1;
        }

        public async Task<List<AttendanceVerifyItemDto>> VerifyAttendanceListAsync(Guid studentId, Guid classId)
        {
            var classEntity = await _db.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
            {
                throw new InvalidOperationException("Class not found");
            }

            var student = await _db.Students
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                throw new InvalidOperationException("Student not found");
            }

            var wallet = student.User?.WalletAddress;
            var onChainClassId = classEntity.OnChainClassId;

            var attendances = await _db.Attendances
                .AsNoTracking()
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Subject)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.TimeSlot)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.Class)
                .Where(a => a.StudentId == studentId && a.Slot.ClassId == classId)
                .OrderBy(a => a.Slot.Date)
                .ThenBy(a => a.RecordedAt)
                .ToListAsync();

            var results = new List<AttendanceVerifyItemDto>(attendances.Count);

            foreach (var attendance in attendances)
            {
                var dto = _mapper.Map<AttendanceDto>(attendance);
                var item = new AttendanceVerifyItemDto
                {
                    Attendance = dto,
                    Verified = false,
                    Message = null
                };

                if (onChainClassId == null || onChainClassId <= 0)
                {
                    item.Message = "Class has no OnChainClassId";
                    results.Add(item);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(wallet))
                {
                    item.Message = "Student has no wallet address";
                    results.Add(item);
                    continue;
                }

                if (!attendance.OnChainRecordId.HasValue || attendance.OnChainRecordId <= 0)
                {
                    item.Message = "Attendance has no OnChainRecordId";
                    results.Add(item);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(attendance.OnChainTransactionHash))
                {
                    item.Message = "Attendance has no OnChainTransactionHash";
                    results.Add(item);
                    continue;
                }

                try
                {
                    // 1) Decode tx receipt and resolve recordId from logs
                    var txHash = attendance.OnChainTransactionHash.Trim();
                    if (!txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        txHash = "0x" + txHash;
                    }

                    IReadOnlyList<(string EventName, string ContractAddress, string DetailJson)> decodedEvents;
                    try
                    {
                        decodedEvents = await _blockchainService.DecodeReceiptEventsAsync(txHash);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decode receipt events for attendance tx {TxHash}", txHash);
                        decodedEvents = Array.Empty<(string, string, string)>();
                    }

                    var attendanceEvents = decodedEvents
                        .Where(e =>
                            string.Equals(e.EventName, "AttendanceMarked", StringComparison.Ordinal) ||
                            string.Equals(e.EventName, "AttendanceUpdated", StringComparison.Ordinal))
                        .ToList();

                    if (attendanceEvents.Count == 0)
                    {
                        item.Message = "Transaction receipt did not contain AttendanceMarked/AttendanceUpdated";
                        results.Add(item);
                        continue;
                    }

                    long? receiptRecordId = null;
                    long? receiptClassId = null;
                    string? receiptStudentAddress = null;

                    // Prefer AttendanceMarked (contains classId + studentAddress) to disambiguate batches
                    foreach (var ev in attendanceEvents.Where(e => string.Equals(e.EventName, "AttendanceMarked", StringComparison.Ordinal)))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(ev.DetailJson);
                            var root = doc.RootElement;
                            if (root.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                            if (!root.TryGetProperty("indexedArgs", out var ia) || ia.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                            var ridStr = ia.TryGetProperty("recordId", out var ridEl) ? ridEl.GetString() : null;
                            var cidStr = ia.TryGetProperty("classId", out var cidEl) ? cidEl.GetString() : null;
                            var saStr = ia.TryGetProperty("studentAddress", out var saEl) ? saEl.GetString() : null;

                            if (!string.IsNullOrWhiteSpace(cidStr) && long.TryParse(cidStr, out var cidLong) && cidLong == onChainClassId.Value &&
                                !string.IsNullOrWhiteSpace(saStr) && string.Equals(saStr, wallet, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrWhiteSpace(ridStr) && long.TryParse(ridStr, out var ridLong) && ridLong > 0)
                            {
                                receiptRecordId = ridLong;
                                receiptClassId = cidLong;
                                receiptStudentAddress = saStr;
                                break;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    // Fallback: any attendance event with recordId
                    if (!receiptRecordId.HasValue)
                    {
                        foreach (var ev in attendanceEvents)
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(ev.DetailJson);
                                var root = doc.RootElement;
                                if (root.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                                if (!root.TryGetProperty("indexedArgs", out var ia) || ia.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                                if (ia.TryGetProperty("recordId", out var ridEl))
                                {
                                    var ridStr = ridEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(ridStr) && long.TryParse(ridStr, out var ridLong) && ridLong > 0)
                                    {
                                        receiptRecordId = ridLong;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    if (!receiptRecordId.HasValue || receiptRecordId.Value <= 0)
                    {
                        item.Message = "Could not resolve recordId from tx receipt";
                        results.Add(item);
                        continue;
                    }

                    // 2) Anti-tamper: receipt-derived recordId must match DB recordId
                    if (attendance.OnChainRecordId.Value != receiptRecordId.Value)
                    {
                        item.Message = "Mismatch: recordId (DB vs tx receipt)";
                        results.Add(item);
                        continue;
                    }

                    // 3) Query chain state using receipt-derived recordId (source of truth)
                    var chainRecord = await _blockchainService.GetAttendanceFromChainAsync(receiptRecordId.Value);

                    if ((long)chainRecord.ClassId != onChainClassId.Value)
                    {
                        item.Message = "Mismatch: classId";
                        results.Add(item);
                        continue;
                    }

                    if (!string.Equals(chainRecord.StudentAddress, wallet, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = "Mismatch: student wallet";
                        results.Add(item);
                        continue;
                    }

                    // Optional: if we decoded student/class from AttendanceMarked, ensure receipt matches too
                    if (receiptClassId.HasValue && receiptClassId.Value != onChainClassId.Value)
                    {
                        item.Message = "Mismatch: classId (tx receipt)";
                        results.Add(item);
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(receiptStudentAddress) && !string.Equals(receiptStudentAddress, wallet, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = "Mismatch: student wallet (tx receipt)";
                        results.Add(item);
                        continue;
                    }

                    var expectedStatus = MapAttendanceToChainStatus(attendance);
                    if (chainRecord.Status != expectedStatus)
                    {
                        item.Message = "Mismatch: status";
                        results.Add(item);
                        continue;
                    }

                    var dbNotes = (attendance.Notes ?? string.Empty).Trim();
                    var chainNotes = (chainRecord.Notes ?? string.Empty).Trim();
                    if (!string.Equals(dbNotes, chainNotes, StringComparison.Ordinal))
                    {
                        item.Message = "Mismatch: notes";
                        results.Add(item);
                        continue;
                    }

                    item.Verified = true;
                    item.Message = "Verified";
                    results.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to verify attendance {AttendanceId} using on-chain record {OnChainRecordId}", attendance.Id, attendance.OnChainRecordId);
                    var msg = ex.Message ?? string.Empty;
                    if (msg.Contains("Record not found", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = "On-chain record not found";
                    }
                    else if (msg.StartsWith("getAttendanceRecord reverted:", StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = msg.Length > 200 ? msg[..200] : msg;
                    }
                    else
                    {
                        item.Message = "Failed to query on-chain record";
                    }
                    results.Add(item);
                }
            }

            return results;
        }

        #endregion
    }
}
