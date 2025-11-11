using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface IEnrollRepository : IGenericRepository<Enroll>
    {
        Task<Enroll?> GetByIdWithDetailsAsync(Guid id);

        Task<(List<Enroll> Enrollments, int TotalCount)> GetPagedEnrollmentsAsync(
            int page,
            int pageSize,
            Guid? classId,
            Guid? studentId,
            bool? isApproved,
            DateTime? registeredFrom,
            DateTime? registeredTo,
            string? sortBy,
            string? sortOrder
        );

        Task<(List<Enroll> Enrollments, int TotalCount)> GetStudentEnrollmentHistoryAsync(
            Guid studentId,
            int page,
            int pageSize,
            Guid? semesterId,
            bool? isApproved,
            string? sortBy,
            string? sortOrder
        );

        Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId);

        Task<List<Enroll>> GetEnrollmentsByClassIdAsync(Guid classId);

        Task<List<Enroll>> GetEnrollmentsByStudentIdAsync(Guid studentId);

        Task<int> GetPendingEnrollmentsCountAsync(Guid classId);

        Task<int> GetApprovedEnrollmentsCountAsync(Guid classId);
    }
}
