using Fap.Domain.DTOs.Subject;
using Fap.Domain.DTOs.Common;

namespace Fap.Api.Interfaces
{
    public interface ISubjectOfferingService
    {
        Task<PagedResult<SubjectOfferingDto>> GetSubjectOfferingsAsync(GetSubjectOfferingsRequest request);
        Task<SubjectOfferingDto?> GetSubjectOfferingByIdAsync(Guid id);
        Task<IEnumerable<SubjectOfferingDto>> GetSubjectOfferingsBySemesterAsync(Guid semesterId);
 Task<IEnumerable<SubjectOfferingDto>> GetSubjectOfferingsBySubjectAsync(Guid subjectId);
    }
}
