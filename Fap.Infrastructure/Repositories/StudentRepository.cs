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
                 .ThenInclude(c => c.SubjectOffering)  // ✅ CHANGED
                   .ThenInclude(so => so.Subject)
          .Include(s => s.Enrolls)
               .ThenInclude(e => e.Class)
            .ThenInclude(c => c.SubjectOffering)
            .ThenInclude(so => so.Semester)
         .Include(s => s.Enrolls)
             .ThenInclude(e => e.Class)
           .ThenInclude(c => c.Teacher)
          .ThenInclude(t => t.User)
            .Include(s => s.ClassMembers)
             .ThenInclude(cm => cm.Class)
           .ThenInclude(c => c.SubjectOffering)  // ✅ CHANGED
             .ThenInclude(so => so.Subject)
                 .Include(s => s.ClassMembers)
           .ThenInclude(cm => cm.Class)
               .ThenInclude(c => c.SubjectOffering)
               .ThenInclude(so => so.Semester)
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

        public async Task<(List<Student> Students, int TotalCount)> GetPagedStudentsAsync(
   int page,
    int pageSize,
   string? searchTerm,
          bool? isGraduated,
            bool? isActive,
      decimal? minGPA,
            decimal? maxGPA,
 string? sortBy,
  string? sortOrder)
        {
            var query = _dbSet
    .Include(s => s.User)
    .Include(s => s.Enrolls)
     .Include(s => s.ClassMembers)
      .AsQueryable();

            // 1. Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s =>
                                s.StudentCode.Contains(searchTerm) ||
                           (s.User != null && s.User.FullName.Contains(searchTerm)) ||
                   (s.User != null && s.User.Email.Contains(searchTerm))
                       );
            }

            if (isGraduated.HasValue)
            {
                query = query.Where(s => s.IsGraduated == isGraduated.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.User != null && s.User.IsActive == isActive.Value);
            }

            if (minGPA.HasValue)
            {
                query = query.Where(s => s.GPA >= minGPA.Value);
            }

            if (maxGPA.HasValue)
            {
                query = query.Where(s => s.GPA <= maxGPA.Value);
            }

            // 2. Get total count before pagination
            var totalCount = await query.CountAsync();

            // 3. Apply sorting
            query = sortBy?.ToLower() switch
            {
                "studentcode" => sortOrder?.ToLower() == "desc"
             ? query.OrderByDescending(s => s.StudentCode)
                   : query.OrderBy(s => s.StudentCode),
                "fullname" => sortOrder?.ToLower() == "desc"
                       ? query.OrderByDescending(s => s.User != null ? s.User.FullName : string.Empty)
               : query.OrderBy(s => s.User != null ? s.User.FullName : string.Empty),
                "gpa" => sortOrder?.ToLower() == "desc"
            ? query.OrderByDescending(s => s.GPA)
                  : query.OrderBy(s => s.GPA),
                "enrollmentdate" => sortOrder?.ToLower() == "desc"
              ? query.OrderByDescending(s => s.EnrollmentDate)
                  : query.OrderBy(s => s.EnrollmentDate),
                _ => query.OrderBy(s => s.StudentCode)
            };

            // 4. Apply pagination
            var students = await query
           .Skip((page - 1) * pageSize)
               .Take(pageSize)
                 .ToListAsync();

            return (students, totalCount);
        }

      /// <summary>
 /// Get students eligible for a specific subject in a semester
  /// Validates: roadmap contains subject, prerequisites met, not already in class
        /// </summary>
    public async Task<(List<Student> Students, int TotalCount)> GetEligibleStudentsForSubjectAsync(
   Guid subjectId,
      Guid semesterId,
        Guid? classId,
    int page,
        int pageSize,
     string? searchTerm)
   {
          // Get subject with prerequisites
   var subject = await _context.Subjects
  .AsNoTracking()
 .FirstOrDefaultAsync(s => s.Id == subjectId);

    if (subject == null)
            {
       return (new List<Student>(), 0);
            }

   // Parse prerequisites
       var prerequisiteCodes = string.IsNullOrWhiteSpace(subject.Prerequisites)
    ? new List<string>()
     : subject.Prerequisites.Split(',', StringSplitOptions.RemoveEmptyEntries)
  .Select(p => p.Trim())
           .ToList();

   // ✅ FIX: Load ALL roadmaps, then filter in memory
      var query = _context.Students
    .Include(s => s.User)
  .Include(s => s.Roadmaps)  // Load ALL roadmaps without filter
   .ThenInclude(r => r.Subject)
    .AsQueryable();

      // Filter: Must have subject in roadmap for this semester with status "Planned"
            query = query.Where(s => s.Roadmaps.Any(r => 
     r.SubjectId == subjectId && 
     r.SemesterId == semesterId &&
 r.Status == "Planned"));

     // Filter: Prerequisites must be completed
  if (prerequisiteCodes.Any())
   {
            query = query.Where(s => 
         prerequisiteCodes.All(prereqCode =>
     s.Roadmaps.Any(r => 
              r.Subject.SubjectCode == prereqCode && 
  r.Status == "Completed")));
     }

       // Filter: Not already in this class (if classId provided)
   if (classId.HasValue)
   {
 query = query.Where(s => !s.ClassMembers.Any(cm => cm.ClassId == classId.Value));
   }

      // Filter: Not graduated
     query = query.Where(s => !s.IsGraduated);

      // Apply search term
if (!string.IsNullOrWhiteSpace(searchTerm))
   {
 query = query.Where(s =>
    s.StudentCode.Contains(searchTerm) ||
              s.User.FullName.Contains(searchTerm) ||
        s.User.Email.Contains(searchTerm));
   }

  var totalCount = await query.CountAsync();

     var students = await query
  .OrderBy(s => s.StudentCode)
      .Skip((page - 1) * pageSize)
          .Take(pageSize)
        .ToListAsync();

   return (students, totalCount);
   }

     /// <summary>
        /// Get students enrolled in a specific semester (have roadmap entries)
      /// </summary>
        public async Task<List<Student>> GetStudentsBySemesterAsync(Guid semesterId)
        {
  return await _context.Students
       .Include(s => s.User)
        .Include(s => s.Roadmaps.Where(r => r.SemesterId == semesterId))
   .Where(s => s.Roadmaps.Any(r => r.SemesterId == semesterId))
          .OrderBy(s => s.StudentCode)
  .ToListAsync();
        }

        /// <summary>
        /// Check if student is eligible to enroll in a subject
        /// Returns eligibility status and reasons if not eligible
   /// </summary>
     public async Task<(bool IsEligible, List<string> Reasons)> CheckSubjectEligibilityAsync(
     Guid studentId,
   Guid subjectId,
   Guid semesterId)
{
      var reasons = new List<string>();

   // Get student with roadmap
      var student = await _context.Students
     .Include(s => s.Roadmaps)
   .ThenInclude(r => r.Subject)
    .FirstOrDefaultAsync(s => s.Id == studentId);

   if (student == null)
 {
         reasons.Add("Student not found");
     return (false, reasons);
    }

            // Check if graduated
     if (student.IsGraduated)
     {
     reasons.Add("Student has already graduated");
     return (false, reasons);
            }

// Check if subject in roadmap for this semester
    var roadmapEntry = student.Roadmaps.FirstOrDefault(r => 
   r.SubjectId == subjectId && r.SemesterId == semesterId);

   if (roadmapEntry == null)
{
     reasons.Add("Subject not in student's roadmap for this semester");
       return (false, reasons);
            }

       if (roadmapEntry.Status == "Completed")
       {
         reasons.Add("Student has already completed this subject");
       return (false, reasons);
      }

 // Get subject with prerequisites
          var subject = await _context.Subjects
    .AsNoTracking()
    .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject != null && !string.IsNullOrWhiteSpace(subject.Prerequisites))
         {
        var prerequisiteCodes = subject.Prerequisites
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(p => p.Trim())
    .ToList();

        var completedSubjectCodes = student.Roadmaps
    .Where(r => r.Status == "Completed")
       .Select(r => r.Subject.SubjectCode)
    .ToHashSet();

       var missingPrerequisites = prerequisiteCodes
      .Where(code => !completedSubjectCodes.Contains(code))
    .ToList();

        if (missingPrerequisites.Any())
       {
   reasons.Add($"Missing prerequisites: {string.Join(", ", missingPrerequisites)}");
   return (false, reasons);
      }
      }

return (true, reasons);
        }
    }
}