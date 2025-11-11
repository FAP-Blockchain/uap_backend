using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class EnrollRepository : GenericRepository<Enroll>, IEnrollRepository
    {
        public EnrollRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Enroll?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Subject)
                        .ThenInclude(s => s.Semester)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<(List<Enroll> Enrollments, int TotalCount)> GetPagedEnrollmentsAsync(
            int page,
            int pageSize,
            Guid? classId,
            Guid? studentId,
            bool? isApproved,
            DateTime? registeredFrom,
            DateTime? registeredTo,
            string? sortBy,
            string? sortOrder)
        {
            var query = _dbSet
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Subject)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .AsQueryable();

            // Filters
            if (classId.HasValue)
                query = query.Where(e => e.ClassId == classId.Value);

            if (studentId.HasValue)
                query = query.Where(e => e.StudentId == studentId.Value);

            if (isApproved.HasValue)
                query = query.Where(e => e.IsApproved == isApproved.Value);

            if (registeredFrom.HasValue)
                query = query.Where(e => e.RegisteredAt >= registeredFrom.Value);

            if (registeredTo.HasValue)
                query = query.Where(e => e.RegisteredAt <= registeredTo.Value);

            var totalCount = await query.CountAsync();

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "registeredat" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.RegisteredAt)
                    : query.OrderBy(e => e.RegisteredAt),

                "studentcode" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Student.StudentCode)
                    : query.OrderBy(e => e.Student.StudentCode),

                "studentname" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Student.User.FullName)
                    : query.OrderBy(e => e.Student.User.FullName),

                "classcode" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Class.ClassCode)
                    : query.OrderBy(e => e.Class.ClassCode),

                "status" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.IsApproved)
                    : query.OrderBy(e => e.IsApproved),

                _ => query.OrderByDescending(e => e.RegisteredAt)
            };

            // Pagination
            var enrollments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (enrollments, totalCount);
        }

        public async Task<(List<Enroll> Enrollments, int TotalCount)> GetStudentEnrollmentHistoryAsync(
            Guid studentId,
            int page,
            int pageSize,
            Guid? semesterId,
            bool? isApproved,
            string? sortBy,
            string? sortOrder)
        {
            var query = _dbSet
                .Include(e => e.Class)
                    .ThenInclude(c => c.Subject)
                        .ThenInclude(s => s.Semester)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                .Where(e => e.StudentId == studentId)
                .AsQueryable();

            // Filters
            if (semesterId.HasValue)
                query = query.Where(e => e.Class.Subject.SemesterId == semesterId.Value);

            if (isApproved.HasValue)
                query = query.Where(e => e.IsApproved == isApproved.Value);

            var totalCount = await query.CountAsync();

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "registeredat" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.RegisteredAt)
                    : query.OrderBy(e => e.RegisteredAt),

                "classcode" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Class.ClassCode)
                    : query.OrderBy(e => e.Class.ClassCode),

                "subjectname" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Class.Subject.SubjectName)
                    : query.OrderBy(e => e.Class.Subject.SubjectName),

                "semester" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(e => e.Class.Subject.Semester.Name)
                    : query.OrderBy(e => e.Class.Subject.Semester.Name),

                _ => query.OrderByDescending(e => e.RegisteredAt)
            };

            var enrollments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (enrollments, totalCount);
        }

        public async Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId)
        {
            return await _dbSet.AnyAsync(e =>
                e.StudentId == studentId &&
                e.ClassId == classId);
        }

        public async Task<List<Enroll>> GetEnrollmentsByClassIdAsync(Guid classId)
        {
            return await _dbSet
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Where(e => e.ClassId == classId)
                .OrderByDescending(e => e.RegisteredAt)
                .ToListAsync();
        }

        public async Task<List<Enroll>> GetEnrollmentsByStudentIdAsync(Guid studentId)
        {
            return await _dbSet
                .Include(e => e.Class)
                    .ThenInclude(c => c.Subject)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.RegisteredAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingEnrollmentsCountAsync(Guid classId)
        {
            return await _dbSet.CountAsync(e =>
                e.ClassId == classId &&
                e.IsApproved == false);
        }

        public async Task<int> GetApprovedEnrollmentsCountAsync(Guid classId)
        {
            return await _dbSet.CountAsync(e =>
                e.ClassId == classId &&
                e.IsApproved == true);
        }
    }
}
