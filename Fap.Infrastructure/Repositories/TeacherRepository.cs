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
                .Include(t => t.TeacherSpecializations)
                    .ThenInclude(ts => ts.Specialization)
                .Include(t => t.Classes)
                    .ThenInclude(c => c.SubjectOffering)
                        .ThenInclude(so => so.Subject)
                .Include(t => t.Classes)
                    .ThenInclude(c => c.SubjectOffering)
                        .ThenInclude(so => so.Semester)
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
                .Include(t => t.TeacherSpecializations)
                    .ThenInclude(ts => ts.Specialization)
                .OrderBy(t => t.TeacherCode)
                .ToListAsync();
        }

        public async Task<(List<Teacher> Teachers, int TotalCount)> GetPagedTeachersAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? specializationKeyword,
            Guid? specializationId,
            bool? isActive,
            string? sortBy,
            string? sortOrder)
        {
            var query = _dbSet
                .Include(t => t.User)
                .Include(t => t.Classes)
                .Include(t => t.TeacherSpecializations)
                    .ThenInclude(ts => ts.Specialization)
                .AsQueryable();

            // 1. Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t =>
                    t.TeacherCode.Contains(searchTerm) ||
                    (t.User != null && t.User.FullName.Contains(searchTerm)) ||
                    (t.User != null && t.User.Email.Contains(searchTerm)) ||
                    (t.Specialization != null && t.Specialization.Contains(searchTerm)) ||
                    t.TeacherSpecializations.Any(ts => ts.Specialization.Name.Contains(searchTerm) || ts.Specialization.Code.Contains(searchTerm)) ||
                    (t.User != null && t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(specializationKeyword))
            {
                query = query.Where(t =>
                    (t.Specialization != null && t.Specialization.Contains(specializationKeyword)) ||
                    t.TeacherSpecializations.Any(ts => ts.Specialization.Name.Contains(specializationKeyword) || ts.Specialization.Code.Contains(specializationKeyword)));
            }

            if (specializationId.HasValue)
            {
                query = query.Where(t => t.TeacherSpecializations.Any(ts => ts.SpecializationId == specializationId.Value));
            }

            if (isActive.HasValue)
            {
                query = query.Where(t => t.User != null && t.User.IsActive == isActive.Value);
            }

            // 2. Get total count
            var totalCount = await query.CountAsync();

            // 3. Apply sorting
            query = sortBy?.ToLower() switch
            {
                "teachercode" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.TeacherCode)
                    : query.OrderBy(t => t.TeacherCode),
                "fullname" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.User != null ? t.User.FullName : string.Empty)
                    : query.OrderBy(t => t.User != null ? t.User.FullName : string.Empty),
                "specialization" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Specialization ?? string.Empty)
                    : query.OrderBy(t => t.Specialization ?? string.Empty),
                "hiredate" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.HireDate)
                    : query.OrderBy(t => t.HireDate),
                "classcount" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Classes.Count)
                    : query.OrderBy(t => t.Classes.Count),
                _ => query.OrderBy(t => t.TeacherCode)
            };

            // 4. Apply pagination
            var teachers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (teachers, totalCount);
        }

        public async Task<List<Guid>> GetSpecializationIdsAsync(Guid teacherId)
        {
            return await _context.TeacherSpecializations
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.SpecializationId)
                .ToListAsync();
        }

        public async Task SetSpecializationsAsync(Guid teacherId, IEnumerable<Guid> specializationIds)
        {
            var existing = await _context.TeacherSpecializations
                .Where(ts => ts.TeacherId == teacherId)
                .ToListAsync();

            _context.TeacherSpecializations.RemoveRange(existing);

            var distinctIds = specializationIds.Distinct().ToList();
            if (distinctIds.Any())
            {
                var newAssignments = distinctIds.Select(id => new TeacherSpecialization
                {
                    TeacherId = teacherId,
                    SpecializationId = id,
                    IsPrimary = false,
                    AssignedAt = DateTime.UtcNow
                });

                await _context.TeacherSpecializations.AddRangeAsync(newAssignments);
            }
        }
    }
}