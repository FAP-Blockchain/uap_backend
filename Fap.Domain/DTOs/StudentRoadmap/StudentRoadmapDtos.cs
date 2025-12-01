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
        public string Message { get; set; } = string.Empty;
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
        
    // Class availability info
    public bool HasAvailableClasses { get; set; }
        public int AvailableClassCount { get; set; }
        public List<AvailableClassInfoDto> AvailableClasses { get; set; } = new();
    }

    /// <summary>
    /// Available class information for a subject
    /// </summary>
    public class AvailableClassInfoDto
    {
        public Guid ClassId { get; set; }
        public string ClassCode { get; set; }
        public string TeacherName { get; set; }
        public int CurrentEnrollment { get; set; }
        public int MaxStudents { get; set; }
        public int AvailableSlots { get; set; }
        public bool IsFull { get; set; }
        public string Schedule { get; set; } = string.Empty;
    }

    /// <summary>
    /// Roadmap generated dynamically from curriculum and academic progress
    /// </summary>
    public class CurriculumRoadmapDto
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public int CurriculumId { get; set; }
        public string CurriculumCode { get; set; }
        public string CurriculumName { get; set; }
        public int TotalSubjects { get; set; }
        public int CompletedSubjects { get; set; }
        public int FailedSubjects { get; set; }
        public int InProgressSubjects { get; set; }
        public int OpenSubjects { get; set; }
        public int LockedSubjects { get; set; }
        public decimal? CurrentGPA { get; set; }
        public List<CurriculumSemesterDto> Semesters { get; set; } = new();
    }

    public class CurriculumSemesterDto
    {
        public int SemesterNumber { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public List<CurriculumSubjectStatusDto> Subjects { get; set; } = new();
    }

    public class CurriculumSubjectStatusDto
    {
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public string Status { get; set; }
        public decimal? FinalScore { get; set; }
        public Guid? CurrentClassId { get; set; }
        public string? CurrentClassCode { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public string? CurrentSemesterName { get; set; }
        public string? PrerequisiteSubjectCode { get; set; }
        public bool PrerequisitesMet { get; set; }
        public decimal? AttendancePercentage { get; set; }
        public bool AttendanceRequirementMet { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Result of checking curriculum-driven eligibility for a subject.
    /// </summary>
    public class SubjectEligibilityResultDto
    {
        public bool IsEligible { get; set; }
        public bool HasCurriculumData { get; set; }
        public bool SubjectInCurriculum { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public bool PrerequisitesMet { get; set; }
        public string? BlockingReason { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    /// <summary>
    /// Graduation eligibility summary derived from curriculum progress.
    /// </summary>
    public class GraduationEligibilityDto
    {
        public Guid StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public int TotalSubjects { get; set; }
        public int CompletedSubjects { get; set; }
        public int FailedSubjects { get; set; }
        public int InProgressSubjects { get; set; }
        public int OpenSubjects { get; set; }
        public int LockedSubjects { get; set; }
        public int RequiredCredits { get; set; }
        public int CompletedCredits { get; set; }
        public List<CurriculumSubjectStatusDto> OutstandingSubjects { get; set; } = new();
        public string? Message { get; set; }
        public DateTime? GraduationDate { get; set; }
    }

    /// <summary>
    /// Request payload to evaluate graduation status with optional persistence.
    /// </summary>
    public class EvaluateGraduationRequest
    {
        public bool MarkAsGraduated { get; set; } = true;
    }
}
