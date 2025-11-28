using System;
using System.Collections.Generic;
using Fap.Domain.DTOs.Slot;

namespace Fap.Domain.DTOs.Class
{
    public class ClassDto
    {
        public Guid Id { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        
        // SubjectOffering reference
        public Guid SubjectOfferingId { get; set; }

        // Subject Info
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        
        // Semester Info
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        
        // Teacher Info
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherCode { get; set; } = string.Empty;
        public string? TeacherEmail { get; set; }
        public string? TeacherPhone { get; set; }
        
        // Class Info
        public int MaxEnrollment { get; set; }
        public int CurrentEnrollment { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClassDetailDto
    {
        public Guid Id { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        
        // SubjectOffering reference
        public Guid SubjectOfferingId { get; set; }
        
        // Subject Info
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        
        // Teacher Info
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherCode { get; set; } = string.Empty;
        public string? TeacherEmail { get; set; }
        
        // Semester Info
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        
        // Class Info
        public int MaxEnrollment { get; set; }
        public int CurrentEnrollment { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Collections
        public List<ClassStudentInfo> Students { get; set; } = new();
        public List<SlotSummaryDto> Slots { get; set; } = new();
        public List<SlotDto> SlotDetails { get; set; } = new();
    }

    public class ClassStudentInfo
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class AssignedStudentInfo
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class SlotSummaryDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TimeSlotName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
