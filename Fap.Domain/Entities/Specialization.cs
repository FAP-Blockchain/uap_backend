using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.Entities
{
    public class Specialization
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(30)]
        public string Code { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TeacherSpecialization> TeacherSpecializations { get; set; } = new List<TeacherSpecialization>();
        public virtual ICollection<SubjectSpecialization> SubjectSpecializations { get; set; } = new List<SubjectSpecialization>();
    }
}
