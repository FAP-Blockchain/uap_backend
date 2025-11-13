using System;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.GradeComponent
{
    public class GradeComponentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int WeightPercent { get; set; }
        public int GradeCount { get; set; }
    }

    public class CreateGradeComponentRequest
    {
        [Required]
        [MaxLength(80, ErrorMessage = "Name cannot exceed 80 characters")]
        public string Name { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100")]
        public int WeightPercent { get; set; }
    }

    public class UpdateGradeComponentRequest
    {
        [Required]
        [MaxLength(80, ErrorMessage = "Name cannot exceed 80 characters")]
        public string Name { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100")]
        public int WeightPercent { get; set; }
    }

    public class GradeComponentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? GradeComponentId { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
