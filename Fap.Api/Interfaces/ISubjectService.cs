using Fap.Domain.DTOs.Subject;

namespace Fap.Api.Interfaces
{
    public interface ISubjectService
    {
    Task<(IEnumerable<SubjectDto> Subjects, int TotalCount)> GetSubjectsAsync(GetSubjectsRequest request);
        Task<SubjectDetailDto?> GetSubjectByIdAsync(Guid id);
        Task<(bool Success, string Message, Guid? SubjectId)> CreateSubjectAsync(CreateSubjectRequest request);
    Task<(bool Success, string Message)> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request);
        Task<(bool Success, string Message)> DeleteSubjectAsync(Guid id);
    }
}
