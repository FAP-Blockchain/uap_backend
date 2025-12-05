using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Attendance;

public class AttendanceOnChainPayloadDto
{
    public string StudentWalletAddress { get; set; } = string.Empty;
    public Guid AttendanceId { get; set; }
    public string AttendanceDataJson { get; set; } = string.Empty;
    public long ExpiresAtUnix { get; set; }
}

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; }
    public string StudentName { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; }
    public DateTime Date { get; set; }
    public string TimeSlotName { get; set; }
    public string ClassCode { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
    public bool IsExcused { get; set; }
    public string? ExcuseReason { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class AttendanceDetailDto : AttendanceDto
{
    public string StudentEmail { get; set; }
    public string TeacherName { get; set; }
    public string SemesterName { get; set; }
    public string SlotStatus { get; set; }

    // Payload cho frontend gọi smart contract
    public AttendanceOnChainPayloadDto? OnChainPayload { get; set; }
}

public class AttendanceStatisticsDto
{
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; }
    public string StudentName { get; set; }
    public int TotalSlots { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<AttendanceDto> AttendanceRecords { get; set; } = new();
}

public class ClassAttendanceReportDto
{
    public Guid ClassId { get; set; }
    public string ClassCode { get; set; }
    public string SubjectName { get; set; }
    public string TeacherName { get; set; }
    public int TotalSlots { get; set; }
    public int TotalStudents { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public List<StudentAttendanceSummary> StudentSummaries { get; set; } = new();
}

public class StudentAttendanceSummary
{
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; }
    public string StudentName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendanceRate { get; set; }
}

// ==================== NEW: SLOT-BASED REQUESTS ====================

/// <summary>
/// Request to take attendance for a slot (RESTful)
/// </summary>
public class TakeSlotAttendanceRequest
{
    [Required]
    public List<StudentAttendanceDto> Students { get; set; } = new();
}

/// <summary>
/// Request to update attendance for a slot
/// </summary>
public class UpdateSlotAttendanceRequest
{
    [Required]
    public List<StudentAttendanceDto> Students { get; set; } = new();
}

public class StudentAttendanceDto
{
    [Required]
    public Guid StudentId { get; set; }
    [Required]
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}

// ==================== NEW: SLOT ATTENDANCE RESPONSE ====================

/// <summary>
/// Response for slot attendance (with all students)
/// </summary>
public class SlotAttendanceDto
{
    public Guid SlotId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassCode { get; set; }
    public string SubjectName { get; set; }
    public DateTime Date { get; set; }
    public string TimeSlotName { get; set; }
    public string TeacherName { get; set; }
    public bool HasAttendance { get; set; }
    public DateTime? RecordedAt { get; set; }
    public List<StudentAttendanceRecord> StudentAttendances { get; set; } = new();
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class StudentAttendanceRecord
{
    public Guid AttendanceId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; }
    public string StudentName { get; set; }
    public string StudentEmail { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool? IsPresent { get; set; }
    public string? Notes { get; set; }
    public bool IsExcused { get; set; }
    public string? ExcuseReason { get; set; }
    // Optional: payload for frontend to call smart contract per attendance record
    public AttendanceOnChainPayloadDto? OnChainPayload { get; set; }
}

// ==================== NEW: PENDING ATTENDANCE ====================

/// <summary>
/// Slot that needs attendance (for teachers)
/// </summary>
public class PendingAttendanceSlotDto
{
    public Guid SlotId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassCode { get; set; }
    public string SubjectName { get; set; }
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string TimeSlotName { get; set; }
    public int TotalStudents { get; set; }
    public int DaysOverdue { get; set; }
}

// ==================== LEGACY REQUESTS (Keep for backward compatibility) ====================

[Obsolete("Use TakeSlotAttendanceRequest instead")]
public class TakeAttendanceRequest
{
    [Required]
    public Guid SlotId { get; set; }
    [Required]
    public List<StudentAttendanceDto> Students { get; set; } = new();
}

public class UpdateAttendanceRequest
{
    [Required]
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}

public class ExcuseAbsenceRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "Excuse reason must be at least 10 characters")]
    [MaxLength(1000, ErrorMessage = "Excuse reason cannot exceed 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}

public class AttendanceFilterRequest
{
    public Guid? ClassId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? SubjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsPresent { get; set; }
    public bool? IsExcused { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SaveAttendanceOnChainRequest
{
    public long OnChainRecordId { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
}

// ==================== BY CLASS STATISTICS ====================

public class ClassAttendanceStatisticsDto
{
    public Guid ClassId { get; set; }
    public string ClassCode { get; set; }
    public string SubjectName { get; set; }
    public int TotalSlots { get; set; }
    public int SlotsWithAttendance { get; set; }
    public int PendingSlots { get; set; }
    public decimal AverageAttendanceRate { get; set; }
}
