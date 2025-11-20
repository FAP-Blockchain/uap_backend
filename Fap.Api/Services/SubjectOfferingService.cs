using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Subject;
using Fap.Domain.DTOs.Common;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fap.Api.Services
{
    public class SubjectOfferingService : ISubjectOfferingService
    {
        private readonly IUnitOfWork _uow;
 private readonly IMapper _mapper;
      private readonly ILogger<SubjectOfferingService> _logger;

   public SubjectOfferingService(IUnitOfWork uow, IMapper mapper, ILogger<SubjectOfferingService> logger)
    {
     _uow = uow;
  _mapper = mapper;
         _logger = logger;
      }

        public async Task<PagedResult<SubjectOfferingDto>> GetSubjectOfferingsAsync(GetSubjectOfferingsRequest request)
        {
       try
 {
         var query = _uow.SubjectOfferings.GetQueryable()
              .Include(so => so.Subject)
   .Include(so => so.Semester)
            .Include(so => so.Classes)
              .ThenInclude(c => c.Members)
    .AsQueryable();

 // Filters
             if (request.SemesterId.HasValue)
  query = query.Where(so => so.SemesterId == request.SemesterId.Value);

  if (request.SubjectId.HasValue)
      query = query.Where(so => so.SubjectId == request.SubjectId.Value);

if (request.IsActive.HasValue)
      query = query.Where(so => so.IsActive == request.IsActive.Value);

    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
      {
     var searchLower = request.SearchTerm.ToLower();
        query = query.Where(so =>
               so.Subject.SubjectCode.ToLower().Contains(searchLower) ||
     so.Subject.SubjectName.ToLower().Contains(searchLower) ||
      so.Semester.Name.ToLower().Contains(searchLower));
 }

var totalCount = await query.CountAsync();

         var offerings = await query
                    .OrderByDescending(so => so.CreatedAt)
           .Skip((request.PageNumber - 1) * request.PageSize)
     .Take(request.PageSize)
         .ToListAsync();

        var dtos = offerings.Select(so => new SubjectOfferingDto
            {
        Id = so.Id,
          SubjectId = so.SubjectId,
    SubjectCode = so.Subject.SubjectCode,
        SubjectName = so.Subject.SubjectName,
         Credits = so.Subject.Credits,
 SemesterId = so.SemesterId,
SemesterName = so.Semester.Name,
          MaxClasses = so.MaxClasses,
           SemesterCapacity = so.SemesterCapacity,
         RegistrationStartDate = so.RegistrationStartDate,
   RegistrationEndDate = so.RegistrationEndDate,
         IsActive = so.IsActive,
   Notes = so.Notes,
                    TotalClasses = so.Classes.Count,
     TotalStudents = so.Classes.Sum(c => c.Members.Count)
         }).ToList();

            return new PagedResult<SubjectOfferingDto>(
   dtos,
          totalCount,
   request.PageNumber,
         request.PageSize
    );
            }
  catch (Exception ex)
            {
           _logger.LogError($"? Error getting subject offerings: {ex.Message}");
throw;
  }
        }

        public async Task<SubjectOfferingDto?> GetSubjectOfferingByIdAsync(Guid id)
        {
  try
 {
     var offering = await _uow.SubjectOfferings.GetByIdWithDetailsAsync(id);
         if (offering == null)
     return null;

           return new SubjectOfferingDto
      {
   Id = offering.Id,
          SubjectId = offering.SubjectId,
     SubjectCode = offering.Subject.SubjectCode,
     SubjectName = offering.Subject.SubjectName,
        Credits = offering.Subject.Credits,
         SemesterId = offering.SemesterId,
        SemesterName = offering.Semester.Name,
         MaxClasses = offering.MaxClasses,
    SemesterCapacity = offering.SemesterCapacity,
          RegistrationStartDate = offering.RegistrationStartDate,
       RegistrationEndDate = offering.RegistrationEndDate,
         IsActive = offering.IsActive,
          Notes = offering.Notes,
    TotalClasses = offering.Classes.Count,
      TotalStudents = offering.Classes.Sum(c => c.Members.Count)
          };
   }
            catch (Exception ex)
 {
         _logger.LogError($"? Error getting subject offering {id}: {ex.Message}");
        throw;
    }
        }

        public async Task<IEnumerable<SubjectOfferingDto>> GetSubjectOfferingsBySemesterAsync(Guid semesterId)
        {
   try
       {
     var offerings = await _uow.SubjectOfferings.GetBySemesterIdAsync(semesterId);

         return offerings.Select(so => new SubjectOfferingDto
        {
           Id = so.Id,
               SubjectId = so.SubjectId,
   SubjectCode = so.Subject.SubjectCode,
    SubjectName = so.Subject.SubjectName,
           Credits = so.Subject.Credits,
     SemesterId = so.SemesterId,
          SemesterName = so.Semester.Name,
        MaxClasses = so.MaxClasses,
             SemesterCapacity = so.SemesterCapacity,
         RegistrationStartDate = so.RegistrationStartDate,
          RegistrationEndDate = so.RegistrationEndDate,
       IsActive = so.IsActive,
               Notes = so.Notes,
TotalClasses = so.Classes.Count,
           TotalStudents = so.Classes.Sum(c => c.Members.Count)
              }).ToList();
            }
            catch (Exception ex)
            {
    _logger.LogError($"? Error getting subject offerings for semester {semesterId}: {ex.Message}");
  throw;
     }
    }

        public async Task<IEnumerable<SubjectOfferingDto>> GetSubjectOfferingsBySubjectAsync(Guid subjectId)
        {
     try
   {
  var offerings = await _uow.SubjectOfferings.GetBySubjectIdAsync(subjectId);

   return offerings.Select(so => new SubjectOfferingDto
   {
            Id = so.Id,
  SubjectId = so.SubjectId,
        SubjectCode = so.Subject.SubjectCode,
    SubjectName = so.Subject.SubjectName,
           Credits = so.Subject.Credits,
           SemesterId = so.SemesterId,
               SemesterName = so.Semester.Name,
   MaxClasses = so.MaxClasses,
        SemesterCapacity = so.SemesterCapacity,
            RegistrationStartDate = so.RegistrationStartDate,
       RegistrationEndDate = so.RegistrationEndDate,
      IsActive = so.IsActive,
        Notes = so.Notes,
          TotalClasses = so.Classes.Count,
    TotalStudents = so.Classes.Sum(c => c.Members.Count)
          }).ToList();
          }
       catch (Exception ex)
            {
      _logger.LogError($"? Error getting subject offerings for subject {subjectId}: {ex.Message}");
                throw;
 }
        }
    }
}
