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
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Student?> GetByStudentCodeAsync(string studentCode)
        {
            return await _dbSet
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentCode == studentCode);
        }

        public async Task<Student?> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Student?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(s => s.User)
                .Include(s => s.Enrolls)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Subject)
                .Include(s => s.Enrolls)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Teacher)
                            .ThenInclude(t => t.User)
                .Include(s => s.ClassMembers)
                    .ThenInclude(cm => cm.Class)
                        .ThenInclude(c => c.Subject)
                .Include(s => s.ClassMembers)
                    .ThenInclude(cm => cm.Class)
                        .ThenInclude(c => c.Teacher)
                            .ThenInclude(t => t.User)
                .Include(s => s.Grades)
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Student>> GetAllWithUsersAsync()
        {
            return await _dbSet
                .Include(s => s.User)
                .Include(s => s.Enrolls)
                .Include(s => s.ClassMembers)
                .OrderBy(s => s.StudentCode)
                .ToListAsync();
        }
    }
}