using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class ClassRepository : GenericRepository<Class>, IClassRepository
    {
        public ClassRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Class?> GetByClassCodeAsync(string classCode)
        {
            return await _dbSet
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Subject)
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Semester)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(c => c.ClassCode == classCode);
        }

        public async Task<Class?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Subject)
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Semester)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Members)
                    .ThenInclude(m => m.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Enrolls)
                    .ThenInclude(e => e.Student)
                        .ThenInclude(s => s.User)
                .Include(c => c.Slots)
                    .ThenInclude(s => s.TimeSlot)
                .Include(c => c.Slots)
                    .ThenInclude(s => s.SubstituteTeacher)
                        .ThenInclude(t => t.User)
                .Include(c => c.Slots)
                    .ThenInclude(s => s.Attendances)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Class>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Subject)
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Semester)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Members)
                .Include(c => c.Enrolls)
                .Include(c => c.Slots)
                .OrderBy(c => c.ClassCode)
                .ToListAsync();
        }

        public async Task<(List<Class> Classes, int TotalCount)> GetPagedClassesAsync(
            int page,
            int pageSize,
            string? subjectId,
            string? teacherId,
            string? semesterId,
            string? classCode,
            string? sortBy,
            string? sortOrder)
        {
            var query = _dbSet
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Subject)
                .Include(c => c.SubjectOffering)
                    .ThenInclude(so => so.Semester)
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .Include(c => c.Members)
                .Include(c => c.Enrolls)
                .Include(c => c.Slots)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(classCode))
            {
                query = query.Where(c => c.ClassCode.Contains(classCode));
            }

            if (Guid.TryParse(subjectId, out var subjectGuid))
            {
                query = query.Where(c => c.SubjectOffering.SubjectId == subjectGuid);
            }

            if (Guid.TryParse(teacherId, out var teacherGuid))
            {
                query = query.Where(c => c.TeacherUserId == teacherGuid);
            }

            if (Guid.TryParse(semesterId, out var semesterGuid))
            {
                query = query.Where(c => c.SubjectOffering.SemesterId == semesterGuid);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "classcode" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.ClassCode)
                    : query.OrderBy(c => c.ClassCode),
                "subjectname" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.SubjectOffering.Subject.SubjectName)
                    : query.OrderBy(c => c.SubjectOffering.Subject.SubjectName),
                "teachername" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.Teacher.User.FullName)
                    : query.OrderBy(c => c.Teacher.User.FullName),
                "studentcount" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.Members.Count)
                    : query.OrderBy(c => c.Members.Count),
                _ => query.OrderBy(c => c.ClassCode)
            };

            // Apply pagination
            var classes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (classes, totalCount);
        }

        public async Task<List<ClassMember>> GetClassRosterAsync(Guid classId)
        {
            return await _context.ClassMembers
                .Include(cm => cm.Student)
                    .ThenInclude(s => s.User)
                .Where(cm => cm.ClassId == classId)
                .OrderBy(cm => cm.Student.StudentCode)
                .ToListAsync();
        }

        public async Task<bool> IsClassCodeUniqueAsync(string classCode, Guid? excludeId = null)
        {
            var query = _dbSet.Where(c => c.ClassCode == classCode);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        // Additional helpers

        public async Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassMembers
                .AnyAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);
        }

        public async Task<ClassMember?> GetClassMemberAsync(Guid classId, Guid studentId)
        {
            return await _context.ClassMembers
                .Include(cm => cm.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(cm => cm.ClassId == classId && cm.StudentId == studentId);
        }

        public async Task AddStudentToClassAsync(ClassMember classMember)
        {
            await _context.ClassMembers.AddAsync(classMember);
        }

        public Task RemoveStudentFromClassAsync(ClassMember classMember)
        {
            _context.ClassMembers.Remove(classMember);
            return Task.CompletedTask;
        }

        public async Task<int> GetCurrentStudentCountAsync(Guid classId)
        {
            return await _context.ClassMembers
                .Where(cm => cm.ClassId == classId)
                .CountAsync();
        }
    }
}
