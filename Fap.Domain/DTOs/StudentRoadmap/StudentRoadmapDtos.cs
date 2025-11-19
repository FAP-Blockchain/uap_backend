using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.StudentRoadmap
{
    /// <summary>
    /// DTO for displaying student roadmap item in list
    /// </summary>
    public class StudentRoadmapDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
        public string SemesterCode { get; set; }
        public int SequenceOrder { get; set; }
        public string Status { get; set; } // "Planned", "InProgress", "Completed", "Failed"
        public decimal? FinalScore { get; set; }
        public string? LetterGrade { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Detailed roadmap information with full subject and semester details
    /// </summary>
    public class StudentRoadmapDetailDto
    {
        public Guid Id { get; set; }

        // Student Info
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }

        // Subject Info
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public string? SubjectDescription { get; set; }

        // Semester Info
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
        public string SemesterCode { get; set; }
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }

        // Roadmap Info
        public int SequenceOrder { get; set; }
        public string Status { get; set; }
        public decimal? FinalScore { get; set; }
        public string? LetterGrade { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Complete roadmap overview with statistics
    /// </summary>
    public class StudentRoadmapOverviewDto
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }

        // Statistics
        public int TotalSubjects { get; set; }
        public int CompletedSubjects { get; set; }
        public int InProgressSubjects { get; set; }
        public int PlannedSubjects { get; set; }
        public int FailedSubjects { get; set; }
        public decimal CompletionPercentage { get; set; }

        // Roadmap items grouped by semester
        public List<SemesterRoadmapGroupDto> SemesterGroups { get; set; } = new();
    }

    /// <summary>
    /// Roadmap items grouped by semester
    /// </summary>
    public class SemesterRoadmapGroupDto
    {
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
        public string SemesterCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrentSemester { get; set; }
        public List<StudentRoadmapDto> Subjects { get; set; } = new();
    }

    /// <summary>
    /// Request to create roadmap entry (Admin only)
    /// </summary>
    public class CreateStudentRoadmapRequest
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        [Required]
        [Range(1, 100)]
        public int SequenceOrder { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Planned"; // Default status

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to update roadmap entry
    /// </summary>
    public class UpdateStudentRoadmapRequest
    {
        public Guid? SemesterId { get; set; }

        public int? SequenceOrder { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [Range(0, 10)]
        public decimal? FinalScore { get; set; }

        [MaxLength(5)]
        public string? LetterGrade { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to get student roadmap with filters
    /// </summary>
    public class GetStudentRoadmapRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public Guid? SemesterId { get; set; }
        public string? SortBy { get; set; } = "sequence";
        public string? SortOrder { get; set; } = "asc";
    }

    /// <summary>
    /// Response for roadmap operations
    /// </summary>
    public class StudentRoadmapResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? RoadmapId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Recommended subjects for enrollment based on roadmap
    /// </summary>
    public class RecommendedSubjectDto
    {
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
        public int SequenceOrder { get; set; }
        public string RecommendationReason { get; set; } // "Next in roadmap", "Prerequisites completed", etc.
        public List<string> Prerequisites { get; set; } = new();
        public bool AllPrerequisitesMet { get; set; }
    }
}
