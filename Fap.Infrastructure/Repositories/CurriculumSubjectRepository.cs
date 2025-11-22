using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Infrastructure.Repositories
{
    public class CurriculumSubjectRepository : GenericRepository<CurriculumSubject>, ICurriculumSubjectRepository
    {
        public CurriculumSubjectRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<CurriculumSubject?> GetByIdAsync(int id)
        {
            return await _context.CurriculumSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.PrerequisiteSubject)
                .Include(cs => cs.Curriculum)
                .FirstOrDefaultAsync(cs => cs.Id == id);
        }

        public async Task<bool> ExistsAsync(int curriculumId, Guid subjectId)
        {
            return await _context.CurriculumSubjects
                .AnyAsync(cs => cs.CurriculumId == curriculumId && cs.SubjectId == subjectId);
        }

        public async Task<bool> IsPrerequisiteForOtherSubjectsAsync(int curriculumId, Guid subjectId)
        {
            return await _context.CurriculumSubjects
                .AnyAsync(cs => cs.CurriculumId == curriculumId && cs.PrerequisiteSubjectId == subjectId);
        }
    }
}
