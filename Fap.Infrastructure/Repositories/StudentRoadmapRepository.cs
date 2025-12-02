using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class StudentRoadmapRepository : GenericRepository<StudentRoadmap>, IStudentRoadmapRepository
    {
        public StudentRoadmapRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<List<StudentRoadmap>> GetStudentRoadmapAsync(Guid studentId)
        {
            return await _context.StudentRoadmaps
                .AsNoTracking()
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId)
                .OrderBy(sr => sr.SequenceOrder)
                .ThenBy(sr => sr.Semester.StartDate)
                .ToListAsync();
        }

        public async Task<List<StudentRoadmap>> GetRoadmapBySemesterAsync(Guid studentId, Guid semesterId)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId && sr.SemesterId == semesterId)
                .OrderBy(sr => sr.SequenceOrder)
                .ToListAsync();
        }

        public async Task<StudentRoadmap?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Student)
                    .ThenInclude(s => s.User)
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        public async Task<List<StudentRoadmap>> GetCurrentSemesterRoadmapAsync(Guid studentId)
        {
            var now = DateTime.UtcNow;

            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr =>
                    sr.StudentId == studentId &&
                    sr.Semester.StartDate <= now &&
                    sr.Semester.EndDate >= now)
                .OrderBy(sr => sr.SequenceOrder)
                .ToListAsync();
        }

        public async Task<List<StudentRoadmap>> GetPlannedSubjectsAsync(Guid studentId, Guid? semesterId = null)
        {
            var query = _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId && sr.Status == "Planned");

            if (semesterId.HasValue)
            {
                query = query.Where(sr => sr.SemesterId == semesterId.Value);
            }

            return await query
                .OrderBy(sr => sr.SequenceOrder)
                .ThenBy(sr => sr.Semester.StartDate)
                .ToListAsync();
        }

        public async Task<List<StudentRoadmap>> GetOpenSubjectsAsync(Guid studentId)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId && sr.Status == "Open")
                .OrderBy(sr => sr.SequenceOrder)
                .ThenBy(sr => sr.Semester.StartDate)
                .ToListAsync();
        }

        public async Task<List<StudentRoadmap>> GetCompletedSubjectsAsync(Guid studentId)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId && sr.Status == "Completed")
                .OrderBy(sr => sr.CompletedAt)
                .ToListAsync();
        }

        public async Task<List<StudentRoadmap>> GetInProgressSubjectsAsync(Guid studentId)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId && sr.Status == "InProgress")
                .OrderBy(sr => sr.SequenceOrder)
                .ToListAsync();
        }

        public async Task<bool> HasRoadmapEntryAsync(Guid studentId, Guid subjectId)
        {
            return await _context.StudentRoadmaps
                .AnyAsync(sr => sr.StudentId == studentId && sr.SubjectId == subjectId);
        }

        public async Task<StudentRoadmap?> GetByStudentAndSubjectAsync(Guid studentId, Guid subjectId)
        {
            return await _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .FirstOrDefaultAsync(sr => sr.StudentId == studentId && sr.SubjectId == subjectId);
        }

        public async Task UpdateRoadmapStatusAsync(
            Guid studentId,
            Guid subjectId,
            string status,
            decimal? finalScore = null,
            string? letterGrade = null)
        {
            var roadmap = await _context.StudentRoadmaps
                .FirstOrDefaultAsync(sr => sr.StudentId == studentId && sr.SubjectId == subjectId);

            if (roadmap != null)
            {
                roadmap.Status = status;
                roadmap.UpdatedAt = DateTime.UtcNow;

                if (status == "InProgress" && roadmap.StartedAt == null)
                {
                    roadmap.StartedAt = DateTime.UtcNow;
                }

                if (status == "Completed")
                {
                    roadmap.CompletedAt = DateTime.UtcNow;
                    roadmap.FinalScore = finalScore;
                    roadmap.LetterGrade = letterGrade ?? string.Empty;
                }

                if (status == "Failed")
                {
                    roadmap.FinalScore = finalScore;
                    roadmap.LetterGrade = letterGrade ?? string.Empty;
                }

                _context.StudentRoadmaps.Update(roadmap);
            }
        }

        public async Task UpdateRoadmapOnEnrollmentAsync(Guid studentId, Guid subjectId, Guid actualSemesterId)
        {
            var roadmap = await _context.StudentRoadmaps
                .FirstOrDefaultAsync(sr => sr.StudentId == studentId && sr.SubjectId == subjectId);

            if (roadmap != null)
            {
                // Update to actual semester and set status to InProgress
                roadmap.SemesterId = actualSemesterId;
                roadmap.Status = "InProgress";
                roadmap.UpdatedAt = DateTime.UtcNow;

                if (roadmap.StartedAt == null)
                {
                    roadmap.StartedAt = DateTime.UtcNow;
                }

                _context.StudentRoadmaps.Update(roadmap);
            }
        }

        public async Task<(int Total, int Completed, int InProgress, int Planned, int Failed)> GetRoadmapStatisticsAsync(Guid studentId)
        {
            var roadmaps = await _context.StudentRoadmaps
                .Where(sr => sr.StudentId == studentId)
                .ToListAsync();

            return (
                Total: roadmaps.Count,
                Completed: roadmaps.Count(r => r.Status == "Completed"),
                InProgress: roadmaps.Count(r => r.Status == "InProgress"),
                Planned: roadmaps.Count(r => r.Status == "Planned"),
                Failed: roadmaps.Count(r => r.Status == "Failed")
            );
        }

        public async Task<(List<StudentRoadmap> Roadmaps, int TotalCount)> GetPagedRoadmapAsync(
            Guid studentId,
            int page,
            int pageSize,
            string? status = null,
            Guid? semesterId = null,
            string? sortBy = null,
            string? sortOrder = null)
        {
            var query = _context.StudentRoadmaps
                .Include(sr => sr.Subject)
                .Include(sr => sr.Semester)
                .Where(sr => sr.StudentId == studentId);

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(sr => sr.Status == status);
            }

            if (semesterId.HasValue)
            {
                query = query.Where(sr => sr.SemesterId == semesterId.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("semester", "desc") => query.OrderByDescending(sr => sr.Semester.StartDate),
                ("semester", _) => query.OrderBy(sr => sr.Semester.StartDate),

                ("subject", "desc") => query.OrderByDescending(sr => sr.Subject.SubjectName),
                ("subject", _) => query.OrderBy(sr => sr.Subject.SubjectName),

                ("status", "desc") => query.OrderByDescending(sr => sr.Status),
                ("status", _) => query.OrderBy(sr => sr.Status),

                ("score", "desc") => query.OrderByDescending(sr => sr.FinalScore),
                ("score", _) => query.OrderBy(sr => sr.FinalScore),

                _ => query.OrderBy(sr => sr.SequenceOrder)
                          .ThenBy(sr => sr.Semester.StartDate)
            };

            // Apply pagination
            var roadmaps = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (roadmaps, totalCount);
        }
    }
}
