using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Subject;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fap.Api.Services
{
    public class SubjectService : ISubjectService
    {
   private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
      private readonly ILogger<SubjectService> _logger;

        public SubjectService(IUnitOfWork uow, IMapper mapper, ILogger<SubjectService> logger)
        {
         _uow = uow;
            _mapper = mapper;
            _logger = logger;
     }

   public async Task<(IEnumerable<SubjectDto> Subjects, int TotalCount)> GetSubjectsAsync(GetSubjectsRequest request)
        {
   var query = await _uow.Subjects.GetAllWithDetailsAsync();

            // Apply search filter
   if (!string.IsNullOrWhiteSpace(request.SearchTerm))
          {
       var searchLower = request.SearchTerm.ToLower();
        query = query.Where(s =>
        s.SubjectCode.ToLower().Contains(searchLower) ||
  s.SubjectName.ToLower().Contains(searchLower)
  );
      }

            // Filter by semester
        if (request.SemesterId.HasValue)
  {
      query = query.Where(s => s.SemesterId == request.SemesterId.Value);
     }

    // Sorting
            query = request.SortBy.ToLower() switch
    {
        "subjectname" => request.IsDescending
       ? query.OrderByDescending(s => s.SubjectName)
              : query.OrderBy(s => s.SubjectName),
     "credits" => request.IsDescending
          ? query.OrderByDescending(s => s.Credits)
        : query.OrderBy(s => s.Credits),
         _ => request.IsDescending
         ? query.OrderByDescending(s => s.SubjectCode)
        : query.OrderBy(s => s.SubjectCode)
          };

      var totalCount = query.Count();

     // Pagination
 var subjects = query
         .Skip((request.PageNumber - 1) * request.PageSize)
    .Take(request.PageSize)
       .Select(s => new SubjectDto
       {
      Id = s.Id,
     SubjectCode = s.SubjectCode,
                  SubjectName = s.SubjectName,
   Credits = s.Credits,
         SemesterId = s.SemesterId,
 SemesterName = s.Semester.Name,
          TotalClasses = s.Classes.Count
       })
        .ToList();

          return (subjects, totalCount);
        }

        public async Task<SubjectDetailDto?> GetSubjectByIdAsync(Guid id)
        {
     var subject = await _uow.Subjects.GetByIdWithDetailsAsync(id);
  if (subject == null) return null;

            return new SubjectDetailDto
            {
     Id = subject.Id,
                SubjectCode = subject.SubjectCode,
    SubjectName = subject.SubjectName,
     Credits = subject.Credits,
             SemesterId = subject.SemesterId,
     SemesterName = subject.Semester.Name,
       SemesterStartDate = subject.Semester.StartDate,
          SemesterEndDate = subject.Semester.EndDate,
         Classes = subject.Classes.Select(c => new ClassSummaryDto
       {
      Id = c.Id,
ClassCode = c.ClassCode,
              TeacherName = c.Teacher?.User?.FullName ?? "N/A",
        CurrentEnrollment = c.Members?.Count ?? 0,
         MaxEnrollment = 0
         }).ToList(),
                TotalStudentsEnrolled = subject.Classes.Sum(c => c.Members?.Count ?? 0)
    };
     }

  public async Task<(bool Success, string Message, Guid? SubjectId)> CreateSubjectAsync(CreateSubjectRequest request)
    {
       try
{
        // Check if subject code already exists
     var existingSubject = await _uow.Subjects.GetBySubjectCodeAsync(request.SubjectCode);
          if (existingSubject != null)
    {
        return (false, $"Subject with code '{request.SubjectCode}' already exists", null);
        }

           // Check if semester exists
  var semester = await _uow.Semesters.GetByIdAsync(request.SemesterId);
                if (semester == null)
    {
        return (false, $"Semester with ID {request.SemesterId} not found", null);
         }

       // Check if semester is closed
         if (semester.IsClosed)
 {
         return (false, "Cannot add subjects to a closed semester", null);
         }

   var subject = new Subject
     {
       Id = Guid.NewGuid(),
       SubjectCode = request.SubjectCode,
       SubjectName = request.SubjectName,
        Credits = request.Credits,
        SemesterId = request.SemesterId
   };

      await _uow.Subjects.AddAsync(subject);
        await _uow.SaveChangesAsync();

     _logger.LogInformation($"✅ Subject created: {subject.SubjectCode}");
      return (true, "Subject created successfully", subject.Id);
    }
            catch (Exception ex)
       {
           _logger.LogError($"❌ Error creating subject: {ex.Message}");
     return (false, "An error occurred while creating the subject", null);
       }
   }

  public async Task<(bool Success, string Message)> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request)
        {
     try
            {
      var subject = await _uow.Subjects.GetByIdAsync(id);
     if (subject == null)
        {
      return (false, "Subject not found");
       }

     // Check if new subject code conflicts with another subject
           if (subject.SubjectCode != request.SubjectCode)
     {
      var existingSubject = await _uow.Subjects.GetBySubjectCodeAsync(request.SubjectCode);
         if (existingSubject != null)
     {
 return (false, $"Subject with code '{request.SubjectCode}' already exists");
   }
         }

  // Check if semester exists
   var semester = await _uow.Semesters.GetByIdAsync(request.SemesterId);
     if (semester == null)
    {
    return (false, $"Semester with ID {request.SemesterId} not found");
        }

     // Check if semester is closed
         if (semester.IsClosed)
      {
   return (false, "Cannot update subjects in a closed semester");
       }

     subject.SubjectCode = request.SubjectCode;
           subject.SubjectName = request.SubjectName;
  subject.Credits = request.Credits;
      subject.SemesterId = request.SemesterId;

       _uow.Subjects.Update(subject);
    await _uow.SaveChangesAsync();

                _logger.LogInformation($"✅ Subject updated: {subject.SubjectCode}");
 return (true, "Subject updated successfully");
            }
            catch (Exception ex)
            {
    _logger.LogError($"❌ Error updating subject: {ex.Message}");
    return (false, "An error occurred while updating the subject");
      }
 }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(Guid id)
        {
    try
            {
     var subject = await _uow.Subjects.GetByIdWithDetailsAsync(id);
 if (subject == null)
           {
    return (false, "Subject not found");
    }

      // Check if subject has classes
    if (subject.Classes != null && subject.Classes.Any())
    {
    return (false, "Cannot delete subject that has existing classes");
            }

                // Check if semester is closed
         if (subject.Semester.IsClosed)
           {
          return (false, "Cannot delete subjects from a closed semester");
          }

        _uow.Subjects.Remove(subject);
       await _uow.SaveChangesAsync();

      _logger.LogInformation($"✅ Subject deleted: {subject.SubjectCode}");
      return (true, "Subject deleted successfully");
 }
       catch (Exception ex)
        {
   _logger.LogError($"❌ Error deleting subject: {ex.Message}");
            return (false, "An error occurred while deleting the subject");
   }
        }
    }
}
