using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fap.Domain.Entities
{
    /// <summary>
    /// Master data for subjects (not tied to a specific semester)
    /// </summary>
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string SubjectCode { get; set; }

        [Required, MaxLength(150)]
        public string SubjectName { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10)]
        public int Credits { get; set; }

        /// <summary>
        /// Subject category: Core, Elective, General, etc.
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Department or faculty offering this subject
        /// </summary>
        [MaxLength(100)]
        public string? Department { get; set; }

        /// <summary>
        /// Prerequisites (comma-separated subject codes)
        /// Example: "MATH101,CS101"
        /// </summary>
        [MaxLength(500)]
        public string? Prerequisites { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation to semester offerings
        public virtual ICollection<SubjectOffering> Offerings { get; set; } = new List<SubjectOffering>();

        // Existing navigations
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<StudentRoadmap> Roadmaps { get; set; } = new List<StudentRoadmap>();
        public virtual ICollection<CurriculumSubject> CurriculumSubjects { get; set; } = new List<CurriculumSubject>();
        public virtual ICollection<SubjectCriteria> SubjectCriterias { get; set; } = new List<SubjectCriteria>();
    }
}
