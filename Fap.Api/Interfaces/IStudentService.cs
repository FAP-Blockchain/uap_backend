using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Student;

namespace Fap.Api.Interfaces
{
    public interface IStudentService
    {
        Task<PagedResult<StudentDto>> GetStudentsAsync(GetStudentsRequest request);
        Task<StudentDetailDto?> GetStudentByIdAsync(Guid id);
        Task<StudentDetailDto?> GetStudentByUserIdAsync(Guid userId);
        /// <summary>
        /// Get students eligible for a specific subject/class
        /// For admin to select students when assigning to a class
        /// </summary>
        Task<PagedResult<StudentDto>> GetEligibleStudentsForClassAsync(
            Guid classId,
            int page = 1,
            int pageSize = 20,
            string? searchTerm = null);
    }
}
