using System;
using System.Collections.Generic;

namespace Fap.Domain.DTOs.Student
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public decimal GPA { get; set; }
        public bool IsGraduated { get; set; }
        public DateTime? GraduationDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalClasses { get; set; }
    }

    public class StudentDetailDto
    {
        public Guid Id { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public decimal GPA { get; set; }
        public bool IsGraduated { get; set; }
        public DateTime? GraduationDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Enrollments (??ng ký l?p)
        public List<EnrollmentInfo> Enrollments { get; set; } = new();
        
        // Classes (L?p ?ang h?c)
        public List<ClassInfo> CurrentClasses { get; set; } = new();
        
        // Statistics
        public int TotalEnrollments { get; set; }
        public int ApprovedEnrollments { get; set; }
        public int PendingEnrollments { get; set; }
        public int TotalClasses { get; set; }
        public int TotalGrades { get; set; }
        public int TotalAttendances { get; set; }
    }

    public class EnrollmentInfo
    {
        public Guid Id { get; set; }
        public string ClassCode { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsApproved { get; set; }
    }

    public class ClassInfo
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int Credits { get; set; }
        public string TeacherName { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
