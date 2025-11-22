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
    public class GradeRepository : GenericRepository<Grade>, IGradeRepository
    {
        public GradeRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Grade?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<List<Grade>> GetGradesByStudentIdAsync(Guid studentId)
        {
            return await _dbSet
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .Where(g => g.StudentId == studentId)
                .OrderBy(g => g.Subject.SubjectCode)
                .ThenBy(g => g.GradeComponent.Name)
                .ToListAsync();
        }

        public async Task<List<Grade>> GetGradesByStudentAndSubjectAsync(Guid studentId, Guid subjectId)
        {
            return await _dbSet
                .Include(g => g.GradeComponent)
                .Where(g => g.StudentId == studentId && g.SubjectId == subjectId)
                .OrderBy(g => g.GradeComponent.Name)
                .ToListAsync();
        }

        public async Task<List<Grade>> GetGradesByClassIdAsync(Guid classId)
        {
            var classEntity = await _context.Classes
                .Include(c => c.SubjectOffering)  // ✅ CHANGED
                    .ThenInclude(so => so.Subject)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
                return new List<Grade>();

            var studentIds = await _context.ClassMembers
                .Where(cm => cm.ClassId == classId)
                .Select(cm => cm.StudentId)
                .ToListAsync();

            return await _dbSet
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.GradeComponent)
                .Where(g => studentIds.Contains(g.StudentId) && g.SubjectId == classEntity.SubjectOffering.SubjectId)  // ✅ CHANGED
                .OrderBy(g => g.Student.StudentCode)
                .ThenBy(g => g.GradeComponent.Name)
                .ToListAsync();
        }

        public async Task<List<Grade>> GetGradesBySubjectIdAsync(Guid subjectId)
        {
            return await _dbSet
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.GradeComponent)
                .Where(g => g.SubjectId == subjectId)
                .OrderBy(g => g.Student.StudentCode)
                .ToListAsync();
        }

        public async Task<Grade?> GetGradeByStudentSubjectComponentAsync(
            Guid studentId,
            Guid subjectId,
            Guid gradeComponentId)
        {
            return await _dbSet
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .FirstOrDefaultAsync(g =>
                    g.StudentId == studentId &&
                    g.SubjectId == subjectId &&
                    g.GradeComponentId == gradeComponentId);
        }

        // ✅ CHANGED: Get grades by semester via SubjectOffering
        public async Task<List<Grade>> GetStudentGradesBySemesterAsync(Guid studentId, Guid semesterId)
        {
            // Get all subject IDs offered in this semester
            var subjectIds = await _context.SubjectOfferings
                .Where(so => so.SemesterId == semesterId && so.IsActive)
                .Select(so => so.SubjectId)
                .ToListAsync();

            return await _dbSet
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .Where(g => g.StudentId == studentId && subjectIds.Contains(g.SubjectId))
                .OrderBy(g => g.Subject.SubjectCode)
                .ThenBy(g => g.GradeComponent.Name)
                .ToListAsync();
        }

        public async Task<bool> HasGradesAsync(Guid studentId, Guid subjectId)
        {
            return await _dbSet
                .AnyAsync(g => g.StudentId == studentId && g.SubjectId == subjectId);
        }

        public async Task<decimal?> CalculateAverageScoreAsync(Guid studentId, Guid subjectId)
        {
            var grades = await _dbSet
                .Include(g => g.GradeComponent)
                .Where(g => g.StudentId == studentId && g.SubjectId == subjectId)
                .ToListAsync();

            if (!grades.Any())
                return null;

            decimal totalWeightedScore = 0;
            int totalWeight = 0;

            foreach (var grade in grades)
            {
                if (!grade.Score.HasValue)
                    continue;
                    
                totalWeightedScore += grade.Score.Value * grade.GradeComponent.WeightPercent;
                totalWeight += grade.GradeComponent.WeightPercent;
            }

            if (totalWeight == 0)
                return null;

            return totalWeightedScore / totalWeight;
        }
    }
}
