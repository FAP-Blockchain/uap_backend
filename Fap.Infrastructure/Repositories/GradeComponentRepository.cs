using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Infrastructure.Repositories
{
    public class GradeComponentRepository : GenericRepository<GradeComponent>, IGradeComponentRepository
    {
        public GradeComponentRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<GradeComponent?> GetByIdWithGradesAsync(Guid id)
        {
            return await _dbSet
                .Include(gc => gc.Grades)
                .FirstOrDefaultAsync(gc => gc.Id == id);
        }

        public async Task<GradeComponent?> GetByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(gc => gc.Name == name);
        }

        public async Task<List<GradeComponent>> GetAllWithGradeCountAsync()
        {
            return await _dbSet
                .Include(gc => gc.Grades)
                .Select(gc => new GradeComponent
                {
                    Id = gc.Id,
                    Name = gc.Name,
                    WeightPercent = gc.WeightPercent,
                    Grades = gc.Grades
                })
                .ToListAsync();
        }

        public async Task<bool> IsComponentInUseAsync(Guid componentId)
        {
            return await _context.Grades
                .AnyAsync(g => g.GradeComponentId == componentId);
        }
    }
}
