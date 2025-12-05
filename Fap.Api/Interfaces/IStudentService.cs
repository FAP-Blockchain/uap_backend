using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Student;

namespace Fap.Api.Interfaces
{
    public interface IStudentService
    {
        Task<PagedResult<StudentDto>> GetStudentsAsync(GetStudentsRequest request);
        Task<StudentDetailDto?> GetStudentByIdAsync(Guid id);
        Task<StudentDetailDto?> GetStudentByUserIdAsync(Guid userId);
        
    
          Task<StudentSelfProfileDto?> GetCurrentStudentProfileAsync(Guid userId);

        Task<PagedResult<StudentDto>> GetEligibleStudentsForClassAsync(
            Guid classId,
            int page = 1,
            int pageSize = 20,
            string? searchTerm = null);
    }
}
