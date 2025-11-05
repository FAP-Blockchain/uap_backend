using Fap.Domain.DTOs.Common;

namespace Fap.Domain.DTOs.User
{
    public class GetUsersRequest : PaginationRequest
    {
        public string? RoleName { get; set; } // Filter by role: "Admin", "Teacher", "Student"
        public bool? IsActive { get; set; } // Filter by active status
    }
}