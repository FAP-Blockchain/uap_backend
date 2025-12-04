using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fap.Domain.Entities
{
    public class SubjectSpecialization
    {
        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; }

        [Required]
        public Guid SpecializationId { get; set; }

        [ForeignKey(nameof(SpecializationId))]
        public virtual Specialization Specialization { get; set; }

        public bool IsRequired { get; set; } = true;
    }
}
