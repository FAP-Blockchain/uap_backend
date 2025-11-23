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

    /// Request to create multiple grades at once
    public class BulkCreateGradesRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one grade is required")]
        public List<CreateGradeRequest> Grades { get; set; } = new();
    }

    public class UpdateGradeRequest
    {
        [Required]
        [Range(0, 10, ErrorMessage = "Score must be between 0 and 10")]
        public decimal Score { get; set; }
               
    }

    /// Request item for bulk update
    public class UpdateGradeItemRequest
    {
        [Required]
        public Guid GradeId { get; set; }

        [Required]
        [Range(0, 10, ErrorMessage = "Score must be between 0 and 10")]
        public decimal Score { get; set; }
    }

    /// Request to update multiple grades at once
    public class BulkUpdateGradesRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one grade is required")]
        public List<UpdateGradeItemRequest> Grades { get; set; } = new();
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

    /// Request to get all grades with filters
    public class GetGradesRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? GradeComponentId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; } = "UpdatedAt";
        public string? SortOrder { get; set; } = "desc";
    }

    /// Response for grade operations
    public class GradeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? GradeId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// Response for bulk grade operations
    public class BulkGradeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<Guid> CreatedGradeIds { get; set; } = new();
        public List<Guid> UpdatedGradeIds { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
