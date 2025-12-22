using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public partial class AttendanceService
    {
    // Slot-based attendance (REST)

        public async Task<SlotAttendanceDto> TakeAttendanceForSlotAsync(Guid slotId, TakeSlotAttendanceRequest request)
        {
            // Get slot with details
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null)
            {
                throw new InvalidOperationException("Slot not found");
            }

            // Check if attendance already exists
            if (await _unitOfWork.Attendances.HasAttendanceForSlotAsync(slotId))
            {
                throw new InvalidOperationException("Attendance has already been taken for this slot");
            }

            // Verify slot status
            if (slot.Status != "Scheduled" && slot.Status != "Completed")
            {
                throw new InvalidOperationException($"Cannot take attendance for a slot with status: {slot.Status}");
            }

            await EnsureAttendanceDateComplianceAsync(slot.Date);

            // Get class with members
            var classEntity = await _unitOfWork.Classes.GetByIdWithDetailsAsync(slot.ClassId);
            if (classEntity == null)
            {
                throw new InvalidOperationException("Class not found");
            }

            var createdAttendances = new List<Attendance>();

            // Take attendance for each student
            foreach (var studentDto in request.Students)
            {
                // Verify student is in class
                var isMember = classEntity.Members?.Any(m => m.StudentId == studentDto.StudentId) ?? false;
                if (!isMember)
                {
                    throw new InvalidOperationException($"Student {studentDto.StudentId} is not a member of this class");
                }

                var attendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    SlotId = slotId,
                    StudentId = studentDto.StudentId,
                    SubjectId = classEntity.SubjectOffering.SubjectId,
                    IsPresent = studentDto.IsPresent,
                    Notes = studentDto.Notes,
                    IsExcused = false,
                    RecordedAt = DateTime.UtcNow
                };

                await _unitOfWork.Attendances.AddAsync(attendance);
                createdAttendances.Add(attendance);
            }

            // Save attendance records only; blockchain is handled client-side
            await _unitOfWork.SaveChangesAsync();

            // Return slot attendance
            return await GetSlotAttendanceAsync(slotId)
                ?? throw new InvalidOperationException("Failed to retrieve slot attendance");
        }

        public async Task<SlotAttendanceDto> UpdateAttendanceForSlotAsync(Guid slotId, UpdateSlotAttendanceRequest request)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId)
                ?? throw new InvalidOperationException("Slot not found");

            await EnsureAttendanceDateComplianceAsync(slot.Date);

            // Get existing attendances
            var existingAttendances = await _unitOfWork.Attendances.GetBySlotIdAsync(slotId);
            if (!existingAttendances.Any())
            {
                throw new InvalidOperationException("No attendance records found for this slot");
            }

            // Update each student's attendance
            foreach (var studentDto in request.Students)
            {
                var attendance = existingAttendances.FirstOrDefault(a => a.StudentId == studentDto.StudentId);
                if (attendance != null)
                {
                    // If changing from absent to present, clear excuse
                    if (studentDto.IsPresent && !attendance.IsPresent)
                    {
                        attendance.IsExcused = false;
                        attendance.ExcuseReason = null;
                    }

                    attendance.IsPresent = studentDto.IsPresent;
                    attendance.Notes = studentDto.Notes;
                    _unitOfWork.Attendances.Update(attendance);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return await GetSlotAttendanceAsync(slotId)
                ?? throw new InvalidOperationException("Failed to retrieve updated attendance");
        }

        public async Task<SlotAttendanceDto?> GetSlotAttendanceAsync(Guid slotId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null) return null;

            var attendances = await _unitOfWork.Attendances.GetBySlotIdAsync(slotId);
            var attendanceList = attendances.ToList();

            // Get all students in class
            var classEntity = await _unitOfWork.Classes.GetByIdWithDetailsAsync(slot.ClassId);
            if (classEntity == null) return null;

            var studentRecords = new List<StudentAttendanceRecord>();

            // Build student attendance records (including optional on-chain payload)
            foreach (var member in classEntity.Members ?? new List<ClassMember>())
            {
                var attendance = attendanceList.FirstOrDefault(a => a.StudentId == member.StudentId);
                var student = member.Student;
                var wallet = student.User?.WalletAddress;

                var record = new StudentAttendanceRecord
                {
                    AttendanceId = attendance?.Id ?? Guid.Empty,
                    StudentId = student?.Id ?? member.StudentId,
                    StudentCode = student?.StudentCode ?? string.Empty,
                    StudentName = student?.User?.FullName ?? string.Empty,
                    StudentEmail = student?.User?.Email ?? string.Empty,
                    ProfileImageUrl = student?.User?.ProfileImageUrl,
                    IsPresent = attendance?.IsPresent,
                    Notes = attendance?.Notes,
                    IsExcused = attendance?.IsExcused ?? false,
                    ExcuseReason = attendance?.ExcuseReason,
                    WalletAddress = wallet
                };

                // Build per-record on-chain payload if wallet and attendance exist
                if (attendance != null && !string.IsNullOrWhiteSpace(wallet))
                {
                    var metadata = new
                    {
                        attendanceId = attendance.Id,
                        studentId = attendance.StudentId,
                        subjectId = attendance.SubjectId,
                        slotId = attendance.SlotId,
                        isPresent = attendance.IsPresent,
                        isExcused = attendance.IsExcused,
                        notes = attendance.Notes,
                        recordedAt = attendance.RecordedAt
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(metadata);

                    record.OnChainPayload = new AttendanceOnChainPayloadDto
                    {
                        StudentWalletAddress = wallet!,
                        AttendanceId = attendance.Id,
                        AttendanceDataJson = json,
                        ExpiresAtUnix = 0
                    };
                }

                studentRecords.Add(record);
            }

            var presentCount = studentRecords.Count(sr => sr.IsPresent == true);
            var absentCount = studentRecords.Count(sr => sr.IsPresent == false);
            var totalStudents = studentRecords.Count;

            return new SlotAttendanceDto
            {
                SlotId = slotId,
                ClassId = slot.ClassId,
                ClassCode = classEntity.ClassCode,
                SubjectName = classEntity.SubjectOffering?.Subject?.SubjectName ?? "Unknown",
                Date = slot.Date,
                TimeSlotName = slot.TimeSlot?.Name ?? "Unknown",
                TeacherName = classEntity.Teacher?.User?.FullName ?? "Unknown",
                HasAttendance = attendanceList.Any(),
                RecordedAt = attendanceList.FirstOrDefault()?.RecordedAt,
                StudentAttendances = studentRecords.OrderBy(sr => sr.StudentCode).ToList(),
                TotalStudents = totalStudents,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                AttendanceRate = totalStudents > 0 ? Math.Round((decimal)presentCount / totalStudents * 100, 2) : 0,
                OnChainClassId = classEntity.OnChainClassId
            };
        }

        public async Task<bool> DeleteSlotAttendanceAsync(Guid slotId)
        {
            var attendances = await _unitOfWork.Attendances.GetBySlotIdAsync(slotId);
            if (!attendances.Any()) return false;

            foreach (var attendance in attendances)
            {
                _unitOfWork.Attendances.Remove(attendance);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<SlotAttendanceDto> MarkAllPresentForSlotAsync(Guid slotId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null)
            {
                throw new InvalidOperationException("Slot not found");
            }

            await EnsureAttendanceDateComplianceAsync(slot.Date);

            var classEntity = await _unitOfWork.Classes.GetByIdWithDetailsAsync(slot.ClassId);
            if (classEntity == null)
            {
                throw new InvalidOperationException("Class not found");
            }

            // Check if attendance already exists
            var hasAttendance = await _unitOfWork.Attendances.HasAttendanceForSlotAsync(slotId);
            if (hasAttendance)
            {
                throw new InvalidOperationException("Attendance already exists. Use update instead.");
            }

            // Mark all students as present
            var students = classEntity.Members?.Select(m => new StudentAttendanceDto
            {
                StudentId = m.StudentId,
                IsPresent = true,
                Notes = "Marked all present"
            }).ToList() ?? new List<StudentAttendanceDto>();

            var request = new TakeSlotAttendanceRequest { Students = students };
            return await TakeAttendanceForSlotAsync(slotId, request);
        }

        public async Task<SlotAttendanceDto> MarkAllAbsentForSlotAsync(Guid slotId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null)
            {
                throw new InvalidOperationException("Slot not found");
            }

            await EnsureAttendanceDateComplianceAsync(slot.Date);

            var classEntity = await _unitOfWork.Classes.GetByIdWithDetailsAsync(slot.ClassId);
            if (classEntity == null)
            {
                throw new InvalidOperationException("Class not found");
            }

            var hasAttendance = await _unitOfWork.Attendances.HasAttendanceForSlotAsync(slotId);
            if (hasAttendance)
            {
                throw new InvalidOperationException("Attendance already exists. Use update instead.");
            }

            var students = classEntity.Members?.Select(m => new StudentAttendanceDto
            {
                StudentId = m.StudentId,
                IsPresent = false,
                Notes = "Marked all absent"
            }).ToList() ?? new List<StudentAttendanceDto>();

            var request = new TakeSlotAttendanceRequest { Students = students };
            return await TakeAttendanceForSlotAsync(slotId, request);
        }

        public async Task<bool> CanTakeAttendanceForSlotAsync(Guid slotId, Guid teacherId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null) return false;

            // Teacher can take attendance if they are the class teacher or substitute
            return slot.Class.TeacherUserId == teacherId ||
               (slot.SubstituteTeacherId.HasValue && slot.SubstituteTeacherId.Value == teacherId);
        }

    // Student view

        public async Task<List<AttendanceDto>> GetStudentAttendanceAsync(Guid studentId, AttendanceFilterRequest? filter = null)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException($"Student with ID {studentId} not found");
            }

            var attendances = await _unitOfWork.Attendances.GetByStudentIdAsync(studentId);
            var query = attendances.AsQueryable();

            // Apply filters
            if (filter != null)
            {
                if (filter.ClassId.HasValue)
                {
                    query = query.Where(a => a.Slot.ClassId == filter.ClassId.Value);
                }

                if (filter.SubjectId.HasValue)
                {
                    query = query.Where(a => a.Slot.Class.SubjectOffering.SubjectId == filter.SubjectId.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(a => a.Slot.Date >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(a => a.Slot.Date <= filter.ToDate.Value);
                }

                if (filter.IsPresent.HasValue)
                {
                    query = query.Where(a => a.IsPresent == filter.IsPresent.Value);
                }

                if (filter.IsExcused.HasValue)
                {
                    query = query.Where(a => a.IsExcused == filter.IsExcused.Value);
                }
            }

            var result = query.OrderByDescending(a => a.Slot.Date).ToList();
            return _mapper.Map<List<AttendanceDto>>(result);
        }

        public async Task<List<AttendanceDto>> GetStudentAttendanceByClassAsync(Guid studentId, Guid classId)
        {
            var filter = new AttendanceFilterRequest { ClassId = classId };
            return await GetStudentAttendanceAsync(studentId, filter);
        }

    // Teacher view

        public async Task<List<PendingAttendanceSlotDto>> GetPendingAttendanceSlotsAsync(Guid teacherId)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                throw new InvalidOperationException($"Teacher with ID {teacherId} not found");
            }

            // Get all slots for teacher
            var slots = await _unitOfWork.Slots.GetByTeacherIdAsync(teacherId);
            var today = DateTime.UtcNow.Date;

            // Filter slots that need attendance
            var pendingSlots = slots
                .Where(s => s.Date <= today &&
                            s.Status == "Scheduled" &&
                            (s.Attendances == null || !s.Attendances.Any()))
                .OrderBy(s => s.Date)
                .ToList();

            var result = new List<PendingAttendanceSlotDto>();

            foreach (var slot in pendingSlots)
            {
                var daysOverdue = (today - slot.Date).Days;

                result.Add(new PendingAttendanceSlotDto
                {
                    SlotId = slot.Id,
                    ClassId = slot.ClassId,
                    ClassCode = slot.Class?.ClassCode ?? "Unknown",
                    SubjectName = slot.Class?.SubjectOffering?.Subject?.SubjectName ?? "Unknown",
                    Date = slot.Date,
                    DayOfWeek = slot.Date.ToString("dddd", CultureInfo.InvariantCulture),
                    StartTime = slot.TimeSlot?.StartTime,
                    EndTime = slot.TimeSlot?.EndTime,
                    TimeSlotName = slot.TimeSlot?.Name ?? "Unknown",
                    TotalStudents = slot.Class?.Members?.Count ?? 0,
                    DaysOverdue = daysOverdue
                });
            }

            return result;
        }

        public async Task<List<ClassAttendanceStatisticsDto>> GetTeacherAttendanceStatisticsAsync(Guid teacherId, Guid? classId = null)
        {
            var teacher = await _unitOfWork.Teachers.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                throw new InvalidOperationException($"Teacher with ID {teacherId} not found");
            }

            // Get all classes taught by teacher
            var allClasses = await _unitOfWork.Classes.GetAllWithDetailsAsync();
            var teacherClasses = allClasses.Where(c => c.TeacherUserId == teacherId).ToList();

            if (classId.HasValue)
            {
                teacherClasses = teacherClasses.Where(c => c.Id == classId.Value).ToList();
            }

            var result = new List<ClassAttendanceStatisticsDto>();

            foreach (var classEntity in teacherClasses)
            {
                var slots = classEntity.Slots?.ToList() ?? new List<Slot>();
                var slotsWithAttendance = slots.Count(s => s.Attendances != null && s.Attendances.Any());
                var attendances = await _unitOfWork.Attendances.GetByClassIdAsync(classEntity.Id);

                // Calculate average attendance rate
                var attendanceList = attendances.ToList();
                var totalAttendances = attendanceList.Count;
                var presentCount = attendanceList.Count(a => a.IsPresent);
                var averageRate = totalAttendances > 0
                    ? Math.Round((decimal)presentCount / totalAttendances * 100, 2)
                    : 0;

                result.Add(new ClassAttendanceStatisticsDto
                {
                    ClassId = classEntity.Id,
                    ClassCode = classEntity.ClassCode,
                    SubjectName = classEntity.SubjectOffering?.Subject?.SubjectName ?? "Unknown",
                    TotalSlots = slots.Count,
                    SlotsWithAttendance = slotsWithAttendance,
                    PendingSlots = slots.Count - slotsWithAttendance,
                    AverageAttendanceRate = averageRate
                });
            }

            return result;
        }
    }

}
