using Fap.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface ICurriculumSubjectRepository : IGenericRepository<CurriculumSubject>
    {
        Task<CurriculumSubject?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int curriculumId, Guid subjectId);
        Task<bool> IsPrerequisiteForOtherSubjectsAsync(int curriculumId, Guid subjectId);
    }
}
