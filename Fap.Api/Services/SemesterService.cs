using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Semester;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;

namespace Fap.Api.Services
{
    public class SemesterService : ISemesterService
    {
   private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
     private readonly ILogger<SemesterService> _logger;

        public SemesterService(IUnitOfWork uow, IMapper mapper, ILogger<SemesterService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
     }

    public async Task<(IEnumerable<SemesterDto> Semesters, int TotalCount)> GetSemestersAsync(GetSemestersRequest request)
   {
  var query = await _uow.Semesters.GetAllWithDetailsAsync();

    // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
  {
        var searchLower = request.SearchTerm.ToLower();
  query = query.Where(s => s.Name.ToLower().Contains(searchLower));
 }

   // Filter by active status
      if (request.IsActive.HasValue)
 {
   var now = DateTime.UtcNow;
      if (request.IsActive.Value)
        {
      query = query.Where(s => s.StartDate <= now && s.EndDate >= now);
           }
       else
   {
           query = query.Where(s => s.StartDate > now || s.EndDate < now);
  }
       }

   // Filter by closed status
 if (request.IsClosed.HasValue)
 {
    query = query.Where(s => s.IsClosed == request.IsClosed.Value);
    }

            // Sorting
     query = request.SortBy.ToLower() switch
 {
    "name" => request.IsDescending
  ? query.OrderByDescending(s => s.Name)
    : query.OrderBy(s => s.Name),
       "enddate" => request.IsDescending
    ? query.OrderByDescending(s => s.EndDate)
      : query.OrderBy(s => s.EndDate),
    _ => request.IsDescending
         ? query.OrderByDescending(s => s.StartDate)
     : query.OrderBy(s => s.StartDate)
         };

   var totalCount = query.Count();

       // Pagination
       var now2 = DateTime.UtcNow;
     var semesters = query
     .Skip((request.PageNumber - 1) * request.PageSize)
     .Take(request.PageSize)
      .Select(s => new SemesterDto
     {
       Id = s.Id,
      Name = s.Name,
        StartDate = s.StartDate,
     EndDate = s.EndDate,
      TotalSubjects = s.Subjects.Count,
 IsActive = s.StartDate <= now2 && s.EndDate >= now2,
     IsClosed = s.IsClosed
        })
       .ToList();

     return (semesters, totalCount);
  }

   public async Task<SemesterDetailDto?> GetSemesterByIdAsync(Guid id)
        {
   var semester = await _uow.Semesters.GetByIdWithDetailsAsync(id);
  if (semester == null) return null;

          var now = DateTime.UtcNow;

       return new SemesterDetailDto
       {
     Id = semester.Id,
    Name = semester.Name,
        StartDate = semester.StartDate,
      EndDate = semester.EndDate,
      IsActive = semester.StartDate <= now && semester.EndDate >= now,
  IsClosed = semester.IsClosed,
            TotalSubjects = semester.Subjects?.Count ?? 0,
     TotalClasses = semester.Subjects?.Sum(s => s.Classes?.Count ?? 0) ?? 0,
      TotalStudentsEnrolled = semester.Subjects?
        .SelectMany(s => s.Classes ?? Enumerable.Empty<Class>())
       .Sum(c => c.Members?.Count ?? 0) ?? 0,
       Subjects = semester.Subjects?.Select(s => new SubjectSummaryDto
     {
     Id = s.Id,
      SubjectCode = s.SubjectCode,
      SubjectName = s.SubjectName,
       Credits = s.Credits,
       TotalClasses = s.Classes?.Count ?? 0
        }).ToList() ?? new List<SubjectSummaryDto>()
       };
        }

        public async Task<(bool Success, string Message, Guid? SemesterId)> CreateSemesterAsync(CreateSemesterRequest request)
        {
  try
  {
   // Validate dates
      if (request.StartDate >= request.EndDate)
{
       return (false, "Start date must be before end date", null);
    }

        if (request.EndDate <= DateTime.UtcNow)
       {
      return (false, "End date must be in the future", null);
       }

    // Check if name already exists
    var existingSemester = await _uow.Semesters.GetByNameAsync(request.Name);
       if (existingSemester != null)
   {
          return (false, $"Semester with name '{request.Name}' already exists", null);
          }

  // Check for overlapping dates
   var hasOverlap = await _uow.Semesters.HasOverlappingDatesAsync(request.StartDate, request.EndDate);
if (hasOverlap)
    {
   return (false, "The date range overlaps with an existing semester", null);
       }

    var semester = new Semester
   {
     Id = Guid.NewGuid(),
       Name = request.Name,
      StartDate = request.StartDate,
    EndDate = request.EndDate,
     IsClosed = false
  };

       await _uow.Semesters.AddAsync(semester);
     await _uow.SaveChangesAsync();

     _logger.LogInformation($"✅ Semester created: {semester.Name}");
     return (true, "Semester created successfully", semester.Id);
   }
  catch (Exception ex)
      {
    _logger.LogError($"❌ Error creating semester: {ex.Message}");
    return (false, "An error occurred while creating the semester", null);
       }
 }

      public async Task<(bool Success, string Message)> UpdateSemesterAsync(Guid id, UpdateSemesterRequest request)
        {
            try
{
      var semester = await _uow.Semesters.GetByIdAsync(id);
  if (semester == null)
      {
        return (false, "Semester not found");
     }

    // Check if semester is closed
    if (semester.IsClosed)
    {
     return (false, "Cannot update a closed semester");
     }

     // Validate dates
      if (request.StartDate >= request.EndDate)
 {
        return (false, "Start date must be before end date");
 }

    // Check if new name conflicts with another semester
   if (semester.Name != request.Name)
       {
   var existingSemester = await _uow.Semesters.GetByNameAsync(request.Name);
      if (existingSemester != null)
   {
         return (false, $"Semester with name '{request.Name}' already exists");
      }
    }

// Check for overlapping dates
    var hasOverlap = await _uow.Semesters.HasOverlappingDatesAsync(
      request.StartDate, 
        request.EndDate, 
       id
      );
 if (hasOverlap)
   {
       return (false, "The date range overlaps with an existing semester");
      }

    semester.Name = request.Name;
     semester.StartDate = request.StartDate;
   semester.EndDate = request.EndDate;

    _uow.Semesters.Update(semester);
     await _uow.SaveChangesAsync();

     _logger.LogInformation($"✅ Semester updated: {semester.Name}");
     return (true, "Semester updated successfully");
   }
     catch (Exception ex)
 {
        _logger.LogError($"❌ Error updating semester: {ex.Message}");
      return (false, "An error occurred while updating the semester");
    }
   }

  public async Task<(bool Success, string Message)> CloseSemesterAsync(Guid id)
        {
       try
  {
    var semester = await _uow.Semesters.GetByIdAsync(id);
    if (semester == null)
    {
      return (false, "Semester not found");
         }

     if (semester.IsClosed)
      {
      return (false, "Semester is already closed");
      }

   // Optional: Add validation that semester has ended
        if (semester.EndDate > DateTime.UtcNow)
    {
      return (false, "Cannot close a semester that hasn't ended yet");
   }

       semester.IsClosed = true;
      _uow.Semesters.Update(semester);
     await _uow.SaveChangesAsync();

     _logger.LogInformation($"✅ Semester closed: {semester.Name}");
     return (true, "Semester closed successfully");
   }
    catch (Exception ex)
      {
    _logger.LogError($"❌ Error closing semester: {ex.Message}");
    return (false, "An error occurred while closing the semester");
       }
        }
    }
}
