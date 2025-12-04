using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Specialization
{
    public class SpecializationDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class SpecializationDetailDto : SpecializationDto
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSpecializationRequest
    {
        [Required, MaxLength(30)]
        public string Code { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateSpecializationRequest
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AssignSpecializationsRequest
    {
        [Required]
        public List<Guid> SpecializationIds { get; set; } = new();
    }
}
