using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fap.Domain.Entities
{
    public class TeacherSpecialization
    {
        [Required]
        public Guid TeacherId { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public virtual Teacher Teacher { get; set; }

        [Required]
        public Guid SpecializationId { get; set; }

        [ForeignKey(nameof(SpecializationId))]
        public virtual Specialization Specialization { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
