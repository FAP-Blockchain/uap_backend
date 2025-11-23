using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
    {
        public SubjectRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode)
        {
            return await _dbSet
                .Include(s => s.Offerings)
                .ThenInclude(o => o.Semester)
                .Include(s => s.SubjectCriterias)
                .FirstOrDefaultAsync(s => s.SubjectCode == subjectCode);
        }

        public async Task<Subject?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(s => s.Offerings)
                .ThenInclude(o => o.Semester)
                .Include(s => s.Offerings)
                    .ThenInclude(o => o.Classes)
                        .ThenInclude(c => c.Teacher)
                            .ThenInclude(t => t.User)
                .Include(s => s.SubjectCriterias)
                .Include(s => s.Roadmaps)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Subject>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(s => s.Offerings)
                .ThenInclude(o => o.Semester)
                .Include(s => s.Offerings)
                    .ThenInclude(o => o.Classes)
                .Include(s => s.SubjectCriterias)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetBySemesterIdAsync(Guid semesterId)
        {
            return await _dbSet
                .Where(s => s.Offerings.Any(o => o.SemesterId == semesterId && o.IsActive))
                .Include(s => s.Offerings.Where(o => o.SemesterId == semesterId))
                    .ThenInclude(o => o.Semester)
                .Include(s => s.Offerings.Where(o => o.SemesterId == semesterId))
                    .ThenInclude(o => o.Classes)
                .Include(s => s.SubjectCriterias)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }
    }
}
