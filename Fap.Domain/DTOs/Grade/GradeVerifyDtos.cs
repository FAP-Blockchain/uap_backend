using System;

namespace Fap.Domain.DTOs.Grade
{
    public class GradeVerifyItemDto
    {
        public GradeDto Grade { get; set; } = new();
        public bool Verified { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
