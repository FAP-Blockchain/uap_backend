using System;

namespace Fap.Domain.DTOs.Enrollment
{
    /// DTO for enrollment list display
    public class EnrollmentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsApproved { get; set; }
        public string Status => IsApproved ? "Approved" : "Pending";
    }

    /// Detailed enrollment information
    public class EnrollmentDetailDto
    {
        public Guid Id { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsApproved { get; set; }
        public string Status => IsApproved ? "Approved" : "Pending";

        // Student Information
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string StudentPhone { get; set; }
        public decimal StudentGPA { get; set; }

        // Class Information
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }

        // Subject Information
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }

        // Teacher Information
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string TeacherEmail { get; set; }

        // Semester Information
        public string SemesterName { get; set; }
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
    }

    /// Student enrollment history item
    public class StudentEnrollmentHistoryDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public string TeacherName { get; set; }
        public string SemesterName { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsApproved { get; set; }
        public string Status => IsApproved ? "Approved" : "Pending";
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
    }
}
