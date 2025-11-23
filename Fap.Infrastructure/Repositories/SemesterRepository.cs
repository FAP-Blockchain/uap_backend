using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class SemesterRepository : GenericRepository<Semester>, ISemesterRepository
    {
        public SemesterRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Semester?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(s => s.SubjectOfferings)
                    .ThenInclude(so => so.Subject)
                .Include(s => s.SubjectOfferings)
                    .ThenInclude(so => so.Classes)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Semester>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(s => s.SubjectOfferings)
                    .ThenInclude(so => so.Subject)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<Semester?> GetByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
        }

        public async Task<Semester?> GetCurrentSemesterAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .FirstOrDefaultAsync(s => s.StartDate <= now && s.EndDate >= now);
        }

        public async Task<bool> HasOverlappingDatesAsync(DateTime startDate, DateTime endDate, Guid? excludeId = null)
        {
            var query = _dbSet.Where(s =>
                (startDate >= s.StartDate && startDate <= s.EndDate) ||
                (endDate >= s.StartDate && endDate <= s.EndDate) ||
                (startDate <= s.StartDate && endDate >= s.EndDate)
            );

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
