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
                                     .ThenInclude(c => c.SubjectOffering)
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
                     .ThenInclude(c => c.SubjectOffering)
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
        // Step 1: Get all curriculum IDs that contain this subject
        var curriculumIds = await _context.CurriculumSubjects
            .Where(cs => cs.SubjectId == subjectId)
            .Select(cs => cs.CurriculumId)
            .Distinct()
            .ToListAsync();

        if (!curriculumIds.Any())
        {
            // No curriculum contains this subject → No eligible students
            return (new List<Student>(), 0);
        }

        // Step 2: Get prerequisite subject ID for this subject in each curriculum
        var curriculumSubjects = await _context.CurriculumSubjects
            .Where(cs => cs.SubjectId == subjectId && curriculumIds.Contains(cs.CurriculumId))
            .Include(cs => cs.PrerequisiteSubject)
            .ToListAsync();

        // Step 3: Build query for eligible students
        var query = _context.Students
            .Include(s => s.User)
            .Include(s => s.Curriculum!)
                .ThenInclude(c => c.CurriculumSubjects)
                    .ThenInclude(cs => cs.Subject)
            .Include(s => s.Grades)
                .ThenInclude(g => g.Subject)
            .Include(s => s.Enrolls)
                .ThenInclude(e => e.Class)
            .AsQueryable();

        // Filter 1: Student must have a curriculum that contains this subject
        query = query.Where(s => s.CurriculumId != null && curriculumIds.Contains(s.CurriculumId.Value));

        // Filter 2: Student must NOT be graduated
        query = query.Where(s => !s.IsGraduated);

        // Filter 3: Student must be active
        query = query.Where(s => s.User.IsActive);

        // Filter 4: Student must NOT have completed this subject (no passing grade)
        query = query.Where(s => !s.Grades.Any(g => 
            g.SubjectId == subjectId && 
            g.GradeComponent.Name.Contains("Final") && 
            g.Score >= 5.0m));

        // Filter 5: Student must NOT be currently enrolled in THIS class
        if (classId.HasValue)
        {
            query = query.Where(s => !s.Enrolls.Any(e => 
                e.ClassId == classId.Value && 
                e.IsApproved));
        }

        // Filter 6: Student must NOT be enrolled in ANY other class for this subject this semester
        query = query.Where(s => !s.Enrolls.Any(e =>
            e.IsApproved &&
            e.Class.SubjectOffering.SubjectId == subjectId &&
            e.Class.SubjectOffering.SemesterId == semesterId));

        // Apply search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s =>
                s.StudentCode.Contains(searchTerm) ||
                s.User.FullName.Contains(searchTerm) ||
                s.User.Email.Contains(searchTerm));
        }

        // Get total count before prerequisite filtering (prerequisite check is complex, done in-memory)
        var candidateStudents = await query
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        // Filter 7: Check prerequisites in-memory (complex logic)
        var eligibleStudents = new List<Student>();

        foreach (var student in candidateStudents)
        {
            if (student.CurriculumId == null)
                continue;

            // Find this subject in student's curriculum
            var curriculumSubject = curriculumSubjects.FirstOrDefault(cs => 
                cs.CurriculumId == student.CurriculumId.Value);

            if (curriculumSubject == null)
                continue;

            // Check if prerequisite is met (if exists)
            if (curriculumSubject.PrerequisiteSubjectId.HasValue)
            {
                var prerequisiteSubjectId = curriculumSubject.PrerequisiteSubjectId.Value;

                // Check if student has passed the prerequisite
                var hasPassedPrerequisite = student.Grades.Any(g =>
                    g.SubjectId == prerequisiteSubjectId &&
                    g.GradeComponent.Name.Contains("Final") &&
                    g.Score >= 5.0m);

                if (!hasPassedPrerequisite)
                {
                    // Prerequisite not met → Skip this student
                    continue;
                }
            }

            // All checks passed → Student is eligible
            eligibleStudents.Add(student);
        }

        // Apply pagination
        var totalCount = eligibleStudents.Count;
        var pagedStudents = eligibleStudents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedStudents, totalCount);
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

        var completedSubjectCodes = student.Roadmaps?
            .Where(r => r.Status == "Completed" && r.Subject != null)
            .Select(r => r.Subject.SubjectCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet()
          ?? new HashSet<string>();

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

    public async Task<Student?> GetWithCurriculumAsync(Guid id)
    {
      return await _dbSet
        .Include(s => s.User)
        .Include(s => s.Curriculum!)
          .ThenInclude(c => c.CurriculumSubjects)
            .ThenInclude(cs => cs.Subject)
        .Include(s => s.Curriculum!)
          .ThenInclude(c => c.CurriculumSubjects)
            .ThenInclude(cs => cs.PrerequisiteSubject)
        .Include(s => s.Grades)
          .ThenInclude(g => g.Subject)
        .Include(s => s.Grades)
          .ThenInclude(g => g.GradeComponent)
        .Include(s => s.Enrolls)
          .ThenInclude(e => e.Class)
            .ThenInclude(c => c.SubjectOffering)
              .ThenInclude(so => so.Subject)
        .Include(s => s.Enrolls)
          .ThenInclude(e => e.Class)
            .ThenInclude(c => c.SubjectOffering)
              .ThenInclude(so => so.Semester)
        .FirstOrDefaultAsync(s => s.Id == id);
    }
    }
}