using Fap.Domain.DTOs.Semester;

namespace Fap.Api.Interfaces
{
    public interface ISemesterService
    {
        Task<(IEnumerable<SemesterDto> Semesters, int TotalCount)> GetSemestersAsync(GetSemestersRequest request);
   Task<SemesterDetailDto?> GetSemesterByIdAsync(Guid id);
     Task<(bool Success, string Message, Guid? SemesterId)> CreateSemesterAsync(CreateSemesterRequest request);
        Task<(bool Success, string Message)> UpdateSemesterAsync(Guid id, UpdateSemesterRequest request);
        Task<(bool Success, string Message)> CloseSemesterAsync(Guid id);
    }
}
