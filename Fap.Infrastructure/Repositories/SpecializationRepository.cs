using System;
using System.Linq;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class SpecializationRepository : GenericRepository<Specialization>, ISpecializationRepository
    {
        public SpecializationRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
        {
            var query = _dbSet.Where(s => s.Code == code);
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IReadOnlyList<Specialization>> GetActiveAsync()
        {
            return await _dbSet
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
