using System;

namespace Fap.Domain.DTOs.Grade
{
    /// DTO for grade display
    public class GradeDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public Guid GradeComponentId { get; set; }
        public string ComponentName { get; set; }
        public int ComponentWeight { get; set; }
        public decimal Score { get; set; }
        public string LetterGrade { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// Detailed grade information
    public class GradeDetailDto
    {
        public Guid Id { get; set; }
        public decimal Score { get; set; }
        public string LetterGrade { get; set; }
        public DateTime UpdatedAt { get; set; }

// Student Information
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }

        // Subject Information
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }

        // Grade Component Information
        public Guid GradeComponentId { get; set; }
        public string ComponentName { get; set; }
        public int ComponentWeight { get; set; }

        // Class Information
        public string ClassName { get; set; }
        public string TeacherName { get; set; }
    }

    /// Class grade report - grades for all students in a class
    public class ClassGradeReportDto
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public List<StudentGradeInClassDto> Students { get; set; } = new();
    }

    public class StudentGradeInClassDto
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public List<ComponentGradeDto> Grades { get; set; } = new();
        public decimal? AverageScore { get; set; }
        public string FinalLetterGrade { get; set; }
    }

    public class ComponentGradeDto
    {
        public Guid? GradeId { get; set; }
        public Guid GradeComponentId { get; set; }
        public string ComponentName { get; set; }
        public int ComponentWeight { get; set; }
        public decimal? Score { get; set; }
        public string LetterGrade { get; set; }
  }

    /// Student grade transcript
    public class StudentGradeTranscriptDto
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public decimal CurrentGPA { get; set; }
        public List<SubjectGradeDto> Subjects { get; set; } = new();
    }

    public class SubjectGradeDto
    {
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public string ClassName { get; set; }
        public string SemesterName { get; set; }
        public List<ComponentGradeDto> ComponentGrades { get; set; } = new();
        public decimal? AverageScore { get; set; }
        public string FinalLetterGrade { get; set; }
    }
}
