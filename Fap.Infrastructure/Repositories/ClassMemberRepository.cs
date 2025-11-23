using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class ClassMemberRepository : GenericRepository<ClassMember>, IClassMemberRepository
    {
        public ClassMemberRepository(FapDbContext context) : base(context) { }

        public async Task<List<ClassMember>> GetByClassIdAsync(Guid classId)
        {
            return await _context.ClassMembers
                .AsNoTracking()
                .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
                .Where(cm => cm.ClassId == classId)
                .OrderBy(cm => cm.JoinedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get class members with full details - for admin display
        /// Always returns fresh data from database
        /// </summary>
        public async Task<List<ClassMember>> GetClassMembersWithDetailsAsync(Guid classId)
        {
            return await _context.ClassMembers
                .AsNoTracking()
                .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
                .Include(cm => cm.Class)
                .ThenInclude(c => c.SubjectOffering)
                .ThenInclude(so => so.Subject)
                .Where(cm => cm.ClassId == classId)
                .OrderBy(cm => cm.Student.StudentCode) // Order by student code
                .ToListAsync();
        }

        public async Task<List<ClassMember>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.ClassMembers
                .AsNoTracking()
                .Include(cm => cm.Class)
                .ThenInclude(c => c.SubjectOffering)
                .ThenInclude(so => so.Subject)
                .Include(cm => cm.Class)
                .ThenInclude(c => c.Teacher)
                .ThenInclude(t => t.User)
                .Where(cm => cm.StudentId == studentId)
                .OrderBy(cm => cm.JoinedAt)
                .ToListAsync();
        }

        public async Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassMembers
                .AsNoTracking()
                .AnyAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);
        }

        public async Task<int> GetClassMemberCountAsync(Guid classId)
        {
            return await _context.ClassMembers
                .AsNoTracking()
                .CountAsync(cm => cm.ClassId == classId);
        }

        public async Task RemoveStudentFromClassAsync(Guid classId, Guid studentId)
        {
            var classMember = await _context.ClassMembers
                .FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);

            if (classMember != null)
            {
                _context.ClassMembers.Remove(classMember);
            }
        }
    }
}
