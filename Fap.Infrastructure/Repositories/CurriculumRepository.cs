using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Infrastructure.Repositories
{
    public class CurriculumRepository : GenericRepository<Curriculum>, ICurriculumRepository
    {
        public CurriculumRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Curriculum?> GetByIdAsync(int id)
        {
            return await _context.Curriculums
                .Include(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.Subject)
                .Include(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.PrerequisiteSubject)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Curriculum?> GetByCodeAsync(string code)
        {
            return await _context.Curriculums
                .Include(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.Subject)
                .Include(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.PrerequisiteSubject)
                .FirstOrDefaultAsync(c => c.Code == code);
        }

        public async Task<IEnumerable<Curriculum>> GetAllWithDetailsAsync()
        {
            return await _context.Curriculums
                .Include(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.Subject)
                .Include(c => c.Students)
                .OrderBy(c => c.Code)
                .ToListAsync();
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _context.Curriculums.Where(c => c.Code == code);
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            
            return await query.AnyAsync();
        }
    }
}
