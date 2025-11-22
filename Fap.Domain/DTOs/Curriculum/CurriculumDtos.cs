using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Curriculum
{
    /// <summary>
    /// Response DTO for curriculum list
    /// </summary>
    public class CurriculumListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalCredits { get; set; }
        public int SubjectCount { get; set; }
        public int StudentCount { get; set; }
    }

    /// <summary>
    /// Response DTO for curriculum detail
    /// </summary>
    public class CurriculumDetailDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalCredits { get; set; }
        public int StudentCount { get; set; }
        public List<CurriculumSubjectDto> Subjects { get; set; } = new();
    }

    /// <summary>
    /// DTO for curriculum subject mapping
    /// </summary>
    public class CurriculumSubjectDto
    {
        public int Id { get; set; }
        public Guid SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int SemesterNumber { get; set; }
        public Guid? PrerequisiteSubjectId { get; set; }
        public string? PrerequisiteSubjectCode { get; set; }
        public string? PrerequisiteSubjectName { get; set; }
    }

    /// <summary>
    /// Request DTO for creating curriculum
    /// </summary>
    public class CreateCurriculumRequest
    {
        [Required(ErrorMessage = "Code is required")]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Total credits is required")]
        [Range(1, 300, ErrorMessage = "Total credits must be between 1 and 300")]
        public int TotalCredits { get; set; }
    }

    /// <summary>
    /// Request DTO for updating curriculum
    /// </summary>
    public class UpdateCurriculumRequest
    {
        [Required(ErrorMessage = "Code is required")]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Total credits is required")]
        [Range(1, 300, ErrorMessage = "Total credits must be between 1 and 300")]
        public int TotalCredits { get; set; }
    }

    /// <summary>
    /// Request DTO for adding subject to curriculum
    /// </summary>
    public class AddSubjectToCurriculumRequest
    {
        [Required(ErrorMessage = "Subject ID is required")]
        public Guid SubjectId { get; set; }

        [Required(ErrorMessage = "Semester number is required")]
        [Range(1, 20, ErrorMessage = "Semester number must be between 1 and 20")]
        public int SemesterNumber { get; set; }

        public Guid? PrerequisiteSubjectId { get; set; }
    }

    /// <summary>
    /// Request DTO for updating curriculum subject
    /// </summary>
    public class UpdateCurriculumSubjectRequest
    {
        [Required(ErrorMessage = "Semester number is required")]
        [Range(1, 20, ErrorMessage = "Semester number must be between 1 and 20")]
        public int SemesterNumber { get; set; }

        public Guid? PrerequisiteSubjectId { get; set; }
    }
}
