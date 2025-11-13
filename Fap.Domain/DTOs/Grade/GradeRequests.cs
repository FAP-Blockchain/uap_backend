using System;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Grade
{
  /// Request to create/update a grade
    public class CreateGradeRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required]
        public Guid GradeComponentId { get; set; }

        [Required]
        [Range(0, 10, ErrorMessage = "Score must be between 0 and 10")]
        public decimal Score { get; set; }

    }

    public class UpdateGradeRequest
    {
        [Required]
        [Range(0, 10, ErrorMessage = "Score must be between 0 and 10")]
        public decimal Score { get; set; }
               
    }

  /// Request to get class grades
    public class GetClassGradesRequest
    {
        public Guid? GradeComponentId { get; set; }
        public string SortBy { get; set; } = "StudentCode";
        public string SortOrder { get; set; } = "asc";
    }

    /// Request to get student grades
    public class GetStudentGradesRequest
    {
        public Guid? SemesterId { get; set; }
        public Guid? SubjectId { get; set; }
        public string SortBy { get; set; } = "SubjectCode";
        public string SortOrder { get; set; } = "asc";
    }

    /// Response for grade operations
    public class GradeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? GradeId { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
