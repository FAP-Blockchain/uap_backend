using System;
using Fap.Domain.DTOs.Common;

namespace Fap.Domain.DTOs.Teacher
{
    public class GetTeachersRequest : PaginationRequest
    {
        public string? SpecializationKeyword { get; set; } // Filter by specialization name/code text
        public Guid? SpecializationId { get; set; }
        public bool? IsActive { get; set; } // Filter by active status
    }
}
