using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Semester;
using Fap.Domain.DTOs.Subject; // ✅ ADD THIS
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
                query = query.Where(s => s.IsActive == request.IsActive.Value);
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
                   TotalSubjects = s.SubjectOfferings.Count, // ✅ FIXED
                   IsActive = s.IsActive,
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
                IsActive = semester.IsActive,
                IsClosed = semester.IsClosed,
                TotalSubjects = semester.SubjectOfferings?.Count ?? 0, // ✅ FIXED
                TotalClasses = semester.SubjectOfferings?.Sum(so => so.Classes?.Count ?? 0) ?? 0, // ✅ FIXED
                TotalStudentsEnrolled = semester.SubjectOfferings? // ✅ FIXED
             .SelectMany(so => so.Classes ?? Enumerable.Empty<Class>())
            .Sum(c => c.Members?.Count ?? 0) ?? 0,
                SubjectOfferings = semester.SubjectOfferings?.Select(so => new SubjectOfferingDto // ✅ FIXED
                {
                    Id = so.Id,
                    SubjectId = so.SubjectId,
                    SubjectCode = so.Subject.SubjectCode,
                    SubjectName = so.Subject.SubjectName,
                    Credits = so.Subject.Credits,
                    SemesterId = so.SemesterId,
                    SemesterName = semester.Name,
                    MaxClasses = so.MaxClasses,
                    SemesterCapacity = so.SemesterCapacity,
                    RegistrationStartDate = so.RegistrationStartDate,
                    RegistrationEndDate = so.RegistrationEndDate,
                    IsActive = so.IsActive,
                    Notes = so.Notes,
                    TotalClasses = so.Classes?.Count ?? 0,
                    TotalStudents = so.Classes?.Sum(c => c.Members?.Count ?? 0) ?? 0
                }).ToList() ?? new List<SubjectOfferingDto>()
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

                // Create semester
                var semester = new Semester
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = false,
                    IsClosed = false
                };

                await _uow.Semesters.AddAsync(semester);
                await _uow.SaveChangesAsync();

                // ✅✅ AUTO-CREATE SubjectOfferings for ALL active subjects
                var activeSubjects = await _uow.Subjects.FindAsync(s => s.IsActive);
                var subjectOfferingsCreated = 0;

                foreach (var subject in activeSubjects)
                {
                    var subjectOffering = new SubjectOffering
                    {
                        Id = Guid.NewGuid(),
                        SubjectId = subject.Id,
                        SemesterId = semester.Id,
                        MaxClasses = request.DefaultMaxClassesPerSubject,
                        SemesterCapacity = request.DefaultSemesterCapacityPerSubject,
                        RegistrationStartDate = request.StartDate.AddDays(-request.RegistrationStartDaysBeforeSemester),
                        RegistrationEndDate = request.StartDate.AddDays(request.RegistrationEndDaysAfterSemesterStart),
                        IsActive = true,
                        Notes = $"Auto-created for {semester.Name}"
                    };

                    await _uow.SubjectOfferings.AddAsync(subjectOffering);
                    subjectOfferingsCreated++;
                }

                await _uow.SaveChangesAsync();

                _logger.LogInformation(
                   "Semester created: {SemesterName}. Auto-created {Count} SubjectOfferings.", 
               semester.Name, 
                  subjectOfferingsCreated);

               return (true, $"Semester created successfully with {subjectOfferingsCreated} subject offerings", semester.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating semester");
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

        public async Task<(bool Success, string Message)> UpdateSemesterActiveStatusAsync(Guid id, bool isActive)
        {
            try
            {
                var semester = await _uow.Semesters.GetByIdAsync(id);
                if (semester == null)
                {
                    return (false, "Semester not found");
                }

                if (semester.IsClosed && isActive)
                {
                    return (false, "Cannot activate a closed semester");
                }

                semester.IsActive = isActive;
                _uow.Semesters.Update(semester);
                await _uow.SaveChangesAsync();

                var state = isActive ? "activated" : "deactivated";
                _logger.LogInformation($"✅ Semester {state}: {semester.Name}");
                return (true, $"Semester {state} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error updating semester active status: {ex.Message}");
                return (false, "An error occurred while updating semester status");
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
