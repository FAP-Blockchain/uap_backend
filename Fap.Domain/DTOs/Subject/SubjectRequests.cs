using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Subject
{
    public class GetSubjectsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    public Guid? SemesterId { get; set; }
        public string SortBy { get; set; } = "SubjectCode"; // SubjectCode, SubjectName, Credits
        public bool IsDescending { get; set; } = false;
    }

    public class CreateSubjectRequest
    {
        [Required(ErrorMessage = "Subject code is required")]
     [MaxLength(50, ErrorMessage = "Subject code cannot exceed 50 characters")]
        public string SubjectCode { get; set; } = string.Empty;

  [Required(ErrorMessage = "Subject name is required")]
     [MaxLength(150, ErrorMessage = "Subject name cannot exceed 150 characters")]
      public string SubjectName { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Credits must be between 1 and 10")]
        public int Credits { get; set; }

  [MaxLength(1000)]
        public string? Description { get; set; }

     [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(500)]
  public string? Prerequisites { get; set; }

        public List<Guid> SpecializationIds { get; set; } = new();

        // ? REMOVED: SemesterId no longer needed
        // Subject is master data, independent of semesters
    // SubjectOfferings are auto-created when creating Semester
    }

    public class UpdateSubjectRequest
    {
  [Required(ErrorMessage = "Subject code is required")]
        [MaxLength(50, ErrorMessage = "Subject code cannot exceed 50 characters")]
      public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject name is required")]
        [MaxLength(150, ErrorMessage = "Subject name cannot exceed 150 characters")]
        public string SubjectName { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Credits must be between 1 and 10")]
        public int Credits { get; set; }

        [MaxLength(1000)]
  public string? Description { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(100)]
   public string? Department { get; set; }

        [MaxLength(500)]
        public string? Prerequisites { get; set; }
        public List<Guid> SpecializationIds { get; set; } = new();
    }
}
