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
    public class TeacherRepository : GenericRepository<Teacher>, ITeacherRepository
    {
        public TeacherRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Teacher?> GetByTeacherCodeAsync(string teacherCode)
        {
            return await _dbSet
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TeacherCode == teacherCode);
        }

        public async Task<Teacher?> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<Teacher?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.Classes)
                    .ThenInclude(c => c.Subject)
                        .ThenInclude(s => s.Semester)
                .Include(t => t.Classes)
                    .ThenInclude(c => c.Members)
                .Include(t => t.Classes)
                    .ThenInclude(c => c.Slots)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Teacher>> GetAllWithUsersAsync()
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.Classes)
                .OrderBy(t => t.TeacherCode)
                .ToListAsync();
        }
    }
}