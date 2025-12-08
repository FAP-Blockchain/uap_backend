using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs; // ServiceResult
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fap.Api.Services
{
    public partial class AttendanceService : IAttendanceService
    {
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IBlockchainService _blockchainService;

        public AttendanceService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidationService validationService,
            IBlockchainService blockchainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validationService = validationService;
            _blockchainService = blockchainService;
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

            EnsureAttendanceDateCompliance(slot.Date);

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

        private void EnsureAttendanceDateCompliance(DateTime slotDate)
        {
            if (_validationService.IsAttendanceDateValidationEnabled)
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

        public async Task<ServiceResult<bool>> SaveAttendanceOnChainAsync(Guid attendanceId, SaveAttendanceOnChainRequest request)
        {
            var attendance = await _unitOfWork.Attendances.GetByIdAsync(attendanceId);
            if (attendance == null)
            {
                return ServiceResult<bool>.Fail("Attendance not found");
            }

            attendance.OnChainRecordId = request.OnChainRecordId;
            attendance.OnChainTransactionHash = request.TransactionHash;
            attendance.IsOnBlockchain = true;

            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResponse(true);
        }

        #endregion
    }
}
