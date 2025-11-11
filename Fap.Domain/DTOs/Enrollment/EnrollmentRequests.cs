using Fap.Domain.DTOs.Common;

namespace Fap.Domain.DTOs.Enrollment
{
    /// Request to create a new enrollment
    public class CreateEnrollmentRequest
    {
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
    }

    /// Response for enrollment operations
    public class EnrollmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? EnrollmentId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// Request to get paginated enrollments with filters
    public class GetEnrollmentsRequest : PaginationRequest
    {
        public Guid? ClassId { get; set; }
        public Guid? StudentId { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime? RegisteredFrom { get; set; }
        public DateTime? RegisteredTo { get; set; }
        public string? SortBy { get; set; } = "RegisteredAt";
        public string? SortOrder { get; set; } = "desc";
    }

    /// Request to approve an enrollment
    public class ApproveEnrollmentRequest
    {
        // Can be extended with additional fields if needed (e.g., ApproverNotes)
    }

    /// Request to reject an enrollment
    public class RejectEnrollmentRequest
    {
        public string? Reason { get; set; }
    }

    /// Request to get student enrollment history
    public class GetStudentEnrollmentsRequest : PaginationRequest
    {
        public Guid? SemesterId { get; set; }
        public bool? IsApproved { get; set; }
        public string? SortBy { get; set; } = "RegisteredAt";
        public string? SortOrder { get; set; } = "desc";
    }
}
