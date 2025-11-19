using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task<Student?> GetByUserIdAsync(Guid userId);
        Task<Student?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Student>> GetAllWithUsersAsync();
        Task<(List<Student> Students, int TotalCount)> GetPagedStudentsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            bool? isGraduated,
            bool? isActive,
            decimal? minGPA,
            decimal? maxGPA,
            string? sortBy,
            string? sortOrder
        );

        /// <summary>
        /// Get students eligible for a specific subject in a semester
        /// Validates: roadmap, prerequisites, not already in class
        /// </summary>
        Task<(List<Student> Students, int TotalCount)> GetEligibleStudentsForSubjectAsync(
            Guid subjectId,
            Guid semesterId,
            Guid? classId,
            int page,
            int pageSize,
            string? searchTerm
        );

        /// <summary>
        /// Get students enrolled in a specific semester (via StudentRoadmap)
        /// </summary>
        Task<List<Student>> GetStudentsBySemesterAsync(Guid semesterId);

        /// <summary>
        /// Check if student is eligible to enroll in a subject
        /// </summary>
        Task<(bool IsEligible, List<string> Reasons)> CheckSubjectEligibilityAsync(
            Guid studentId,
            Guid subjectId,
            Guid semesterId
        );
    }
}