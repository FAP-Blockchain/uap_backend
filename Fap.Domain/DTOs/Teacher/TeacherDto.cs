using System;
using System.Collections.Generic;

namespace Fap.Domain.DTOs.Teacher
{
    public class TeacherDto
    {
        public Guid Id { get; set; }
        public string TeacherCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }
        public string? Specialization { get; set; }
        public string? PhoneNumber { get; set; }  // ? Now from User.PhoneNumber
        public bool IsActive { get; set; }
        public int TotalClasses { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class TeacherDetailDto
    {
        public Guid Id { get; set; }
        public string TeacherCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }
        public string? Specialization { get; set; }
        public string? PhoneNumber { get; set; }  // ? Now from User.PhoneNumber
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProfileImageUrl { get; set; }
        
        // Classes (L?p ?ang d?y)
        public List<TeachingClassInfo> Classes { get; set; } = new();
        
        // Statistics
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; } // T?ng s? SV trong t?t c? c�c l?p
    }

    public class TeachingClassInfo
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int Credits { get; set; }
        public string SemesterName { get; set; }
        public int TotalStudents { get; set; } // S? SV trong l?p n�y
        public int TotalSlots { get; set; }     // S? bu?i h?c
    }
}
