using Fap.Domain.DTOs.Attendance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IAttendanceService
    {
        // ==================== SLOT-BASED ATTENDANCE (RESTful) ====================
       
        /// <summary>
        /// Take attendance for a slot (RESTful)
        /// </summary>
        Task<SlotAttendanceDto> TakeAttendanceForSlotAsync(Guid slotId, TakeSlotAttendanceRequest request);
        
        /// <summary>
        /// Update attendance for a slot
        /// </summary>
        Task<SlotAttendanceDto> UpdateAttendanceForSlotAsync(Guid slotId, UpdateSlotAttendanceRequest request);
        
        /// <summary>
        /// Get attendance for a specific slot
        /// </summary>
        Task<SlotAttendanceDto?> GetSlotAttendanceAsync(Guid slotId);

        /// <summary>
        /// Get detailed attendance information (including on-chain payload) by attendance id
        /// </summary>
        Task<AttendanceDetailDto?> GetAttendanceDetailByIdAsync(Guid attendanceId);
        
        /// <summary>
        /// Delete attendance for a slot (if wrong)
        /// </summary>
        Task<bool> DeleteSlotAttendanceAsync(Guid slotId);
  
        /// <summary>
        /// Mark all students as present for a slot
        /// </summary>
        Task<SlotAttendanceDto> MarkAllPresentForSlotAsync(Guid slotId);
        
        /// <summary>
        /// Mark all students as absent for a slot
        /// </summary>
        Task<SlotAttendanceDto> MarkAllAbsentForSlotAsync(Guid slotId);
        
        /// <summary>
        /// Check if teacher can take attendance for a slot
        /// </summary>
        Task<bool> CanTakeAttendanceForSlotAsync(Guid slotId, Guid teacherId);

        // ==================== STUDENT VIEW ====================
        
        /// <summary>
        /// Get student's attendance history
        /// </summary>
        Task<List<AttendanceDto>> GetStudentAttendanceAsync(Guid studentId, AttendanceFilterRequest? filter = null);
        
        /// <summary>
        /// Get student's attendance statistics
        /// </summary>
        Task<AttendanceStatisticsDto> GetStudentAttendanceStatisticsAsync(Guid studentId, Guid? classId = null);
        
        /// <summary>
        /// Get student's attendance by class
        /// </summary>
        Task<List<AttendanceDto>> GetStudentAttendanceByClassAsync(Guid studentId, Guid classId);

        // ==================== TEACHER VIEW ====================
  
        /// <summary>
        /// Get pending attendance slots for teacher
        /// </summary>
        Task<List<PendingAttendanceSlotDto>> GetPendingAttendanceSlotsAsync(Guid teacherId);
        
        /// <summary>
        /// Get teacher's attendance statistics
        /// </summary>
        Task<List<ClassAttendanceStatisticsDto>> GetTeacherAttendanceStatisticsAsync(Guid teacherId, Guid? classId = null);

        // ==================== CLASS REPORTS ====================
        
        /// <summary>
        /// Get attendance report for a class
        /// </summary>
        Task<ClassAttendanceReportDto> GetClassAttendanceReportAsync(Guid classId);

        // ==================== LEGACY METHODS (Keep for backward compatibility) ====================
        
        Task<AttendanceDto?> GetAttendanceByIdAsync(Guid id);
        Task<IEnumerable<AttendanceDto>> GetAttendancesBySlotIdAsync(Guid slotId);
        Task<IEnumerable<AttendanceDto>> GetAttendancesByClassIdAsync(Guid classId);
        Task<IEnumerable<AttendanceDto>> GetAttendancesByStudentIdAsync(Guid studentId);
        
        [Obsolete("Use TakeAttendanceForSlotAsync instead")]
        Task<IEnumerable<AttendanceDto>> TakeAttendanceAsync(TakeAttendanceRequest request);
        
        Task<AttendanceDto?> UpdateAttendanceAsync(Guid id, UpdateAttendanceRequest request);
        Task<AttendanceDto?> ExcuseAbsenceAsync(Guid id, ExcuseAbsenceRequest request);
        Task<IEnumerable<AttendanceDto>> GetAttendancesByFilterAsync(AttendanceFilterRequest filter);
     
        // Validation
        Task<bool> CanTakeAttendanceAsync(Guid slotId, Guid teacherUserId);
        Task<bool> CanExcuseAbsenceAsync(Guid attendanceId, Guid studentUserId);

        // On-chain integration (frontend-driven)
        Task<Fap.Domain.DTOs.ServiceResult<bool>> SaveAttendanceOnChainAsync(Guid attendanceId, SaveAttendanceOnChainRequest request);
    }
}
