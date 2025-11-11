using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Enrollment;

namespace Fap.Api.Interfaces
{
    public interface IEnrollmentService
    {
       /// Student enrolls in a class
       Task<EnrollmentResponse> CreateEnrollmentAsync(CreateEnrollmentRequest request);

       /// Get enrollment details by ID
       Task<EnrollmentDetailDto?> GetEnrollmentByIdAsync(Guid id);

       /// Get paginated list of enrollments with filters
       Task<PagedResult<EnrollmentDto>> GetEnrollmentsAsync(GetEnrollmentsRequest request);

       /// Admin approves an enrollment
       Task<EnrollmentResponse> ApproveEnrollmentAsync(Guid id);

       /// Admin rejects an enrollment
       Task<EnrollmentResponse> RejectEnrollmentAsync(Guid id, string? reason);

       /// Student drops/cancels their enrollment
       Task<EnrollmentResponse> DropEnrollmentAsync(Guid id, Guid studentId);

       /// Get student's enrollment history
       Task<PagedResult<StudentEnrollmentHistoryDto>> GetStudentEnrollmentHistoryAsync(
           Guid studentId,
           GetStudentEnrollmentsRequest request);
    }
}
