using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.StudentRoadmap;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Fap.Api.Services
{
    public class StudentRoadmapService : IStudentRoadmapService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentRoadmapService> _logger;

        public StudentRoadmapService(
        IUnitOfWork uow,
           IMapper mapper,
                 ILogger<StudentRoadmapService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // ==================== STUDENT APIs ====================

        public async Task<StudentRoadmapOverviewDto?> GetMyRoadmapAsync(Guid studentId)
        {
            try
            {
                var student = await _uow.Students.GetByIdWithDetailsAsync(studentId);
                if (student == null)
                    return null;

                var roadmaps = await _uow.StudentRoadmaps.GetStudentRoadmapAsync(studentId);
                var stats = await _uow.StudentRoadmaps.GetRoadmapStatisticsAsync(studentId);

                var overview = new StudentRoadmapOverviewDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    StudentName = student.User.FullName,
                    TotalSubjects = stats.Total,
                    CompletedSubjects = stats.Completed,
                    InProgressSubjects = stats.InProgress,
                    PlannedSubjects = stats.Planned,
                    FailedSubjects = stats.Failed,
                    CompletionPercentage = stats.Total > 0
            ? Math.Round((decimal)stats.Completed / stats.Total * 100, 2)
                  : 0
                };

                // Group by semester
                var semesterGroups = roadmaps
                .GroupBy(r => new { r.SemesterId, r.Semester })
                .Select(g => new SemesterRoadmapGroupDto
                {
                    SemesterId = g.Key.SemesterId,
                    SemesterName = g.Key.Semester.Name,
                    SemesterCode = g.Key.Semester.Name,
                    StartDate = g.Key.Semester.StartDate,
                    EndDate = g.Key.Semester.EndDate,
                    IsCurrentSemester = DateTime.UtcNow >= g.Key.Semester.StartDate
             && DateTime.UtcNow <= g.Key.Semester.EndDate,
                    Subjects = _mapper.Map<List<StudentRoadmapDto>>(g.OrderBy(r => r.SequenceOrder).ToList())
                })
                  .OrderBy(s => s.StartDate)
               .ToList();

                overview.SemesterGroups = semesterGroups;
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<List<StudentRoadmapDto>> GetRoadmapBySemesterAsync(Guid studentId, Guid semesterId)
        {
            try
            {
                var roadmaps = await _uow.StudentRoadmaps.GetRoadmapBySemesterAsync(studentId, semesterId);
                return _mapper.Map<List<StudentRoadmapDto>>(roadmaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for student {StudentId} semester {SemesterId}",
                         studentId, semesterId);
                throw;
            }
        }

        public async Task<List<StudentRoadmapDto>> GetCurrentSemesterRoadmapAsync(Guid studentId)
        {
            try
            {
                var roadmaps = await _uow.StudentRoadmaps.GetCurrentSemesterRoadmapAsync(studentId);
                return _mapper.Map<List<StudentRoadmapDto>>(roadmaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current semester roadmap for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<List<RecommendedSubjectDto>> GetRecommendedSubjectsAsync(Guid studentId)
        {
            try
            {
                // Get planned subjects for current or next semester
                var plannedSubjects = await _uow.StudentRoadmaps.GetPlannedSubjectsAsync(studentId);

                var recommendations = new List<RecommendedSubjectDto>();
                var now = DateTime.UtcNow;

                foreach (var roadmap in plannedSubjects.Take(5)) // Top 5 recommendations
                {
                    var recommendation = new RecommendedSubjectDto
                    {
                        SubjectId = roadmap.SubjectId,
                        SubjectCode = roadmap.Subject.SubjectCode,
                        SubjectName = roadmap.Subject.SubjectName,
                        Credits = roadmap.Subject.Credits,
                        SemesterId = roadmap.SemesterId,
                        SemesterName = roadmap.Semester.Name,
                        SequenceOrder = roadmap.SequenceOrder,
                        AllPrerequisitesMet = true, // TODO: Implement prerequisite check
                        RecommendationReason = roadmap.Semester.StartDate <= now && roadmap.Semester.EndDate >= now
    ? "Planned for current semester"
    : "Next in your roadmap"
                    };

                    recommendations.Add(recommendation);
                }

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended subjects for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<PagedResult<StudentRoadmapDto>> GetPagedRoadmapAsync(
                Guid studentId,
          GetStudentRoadmapRequest request)
        {
            try
            {
                var (roadmaps, totalCount) = await _uow.StudentRoadmaps.GetPagedRoadmapAsync(
                        studentId,
                        request.Page,
                 request.PageSize,
                  request.Status,
                      request.SemesterId,
                   request.SortBy,
                      request.SortOrder
                 );

                var roadmapDtos = _mapper.Map<List<StudentRoadmapDto>>(roadmaps);

                return new PagedResult<StudentRoadmapDto>(
               roadmapDtos,
             totalCount,
              request.Page,
               request.PageSize
          );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged roadmap for student {StudentId}", studentId);
                throw;
            }
        }

        // ==================== ADMIN APIs ====================

        public async Task<StudentRoadmapDetailDto?> GetRoadmapByIdAsync(Guid id)
        {
            try
            {
                var roadmap = await _uow.StudentRoadmaps.GetByIdWithDetailsAsync(id);
                if (roadmap == null)
                    return null;

                return _mapper.Map<StudentRoadmapDetailDto>(roadmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap {RoadmapId}", id);
                throw;
            }
        }

        public async Task<StudentRoadmapResponse> CreateRoadmapAsync(CreateStudentRoadmapRequest request)
        {
            var response = new StudentRoadmapResponse();

            try
            {
                // Validate student exists
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null)
                {
                    response.Errors.Add($"Student with ID '{request.StudentId}' not found");
                    response.Message = "Roadmap creation failed";
                    return response;
                }

                // Validate subject exists
                var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    response.Errors.Add($"Subject with ID '{request.SubjectId}' not found");
                    response.Message = "Roadmap creation failed";
                    return response;
                }

                // Validate semester exists
                var semester = await _uow.Semesters.GetByIdAsync(request.SemesterId);
                if (semester == null)
                {
                    response.Errors.Add($"Semester with ID '{request.SemesterId}' not found");
                    response.Message = "Roadmap creation failed";
                    return response;
                }

                // Check if roadmap entry already exists
                var exists = await _uow.StudentRoadmaps.HasRoadmapEntryAsync(
           request.StudentId,
                request.SubjectId);

                if (exists)
                {
                    response.Errors.Add("Student already has this subject in their roadmap");
                    response.Message = "Roadmap creation failed";
                    return response;
                }

                // Create roadmap entry
                var roadmap = new StudentRoadmap
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    SubjectId = request.SubjectId,
                    SemesterId = request.SemesterId,
                    SequenceOrder = request.SequenceOrder,
                    Status = request.Status,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.StudentRoadmaps.AddAsync(roadmap);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Roadmap entry created successfully";
                response.RoadmapId = roadmap.Id;

                _logger.LogInformation(
                  "Created roadmap entry for student {StudentId}, subject {SubjectId}",
                   request.StudentId, request.SubjectId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating roadmap entry");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Roadmap creation failed";
                return response;
            }
        }

        public async Task<StudentRoadmapResponse> UpdateRoadmapAsync(Guid id, UpdateStudentRoadmapRequest request)
        {
            var response = new StudentRoadmapResponse { RoadmapId = id };

            try
            {
                var roadmap = await _uow.StudentRoadmaps.GetByIdAsync(id);
                if (roadmap == null)
                {
                    response.Errors.Add($"Roadmap with ID '{id}' not found");
                    response.Message = "Roadmap update failed";
                    return response;
                }

                // Update fields if provided
                if (request.SemesterId.HasValue)
                {
                    var semester = await _uow.Semesters.GetByIdAsync(request.SemesterId.Value);
                    if (semester == null)
                    {
                        response.Errors.Add($"Semester with ID '{request.SemesterId}' not found");
                        response.Message = "Roadmap update failed";
                        return response;
                    }
                    roadmap.SemesterId = request.SemesterId.Value;
                }

                if (request.SequenceOrder.HasValue)
                    roadmap.SequenceOrder = request.SequenceOrder.Value;

                if (!string.IsNullOrEmpty(request.Status))
                {
                    roadmap.Status = request.Status;

                    // Auto-set timestamps based on status
                    if (request.Status == "InProgress" && roadmap.StartedAt == null)
                        roadmap.StartedAt = DateTime.UtcNow;

                    if (request.Status == "Completed" && roadmap.CompletedAt == null)
                        roadmap.CompletedAt = DateTime.UtcNow;
                }

                if (request.FinalScore.HasValue)
                    roadmap.FinalScore = request.FinalScore.Value;

                if (!string.IsNullOrEmpty(request.LetterGrade))
                    roadmap.LetterGrade = request.LetterGrade;

                if (request.Notes != null)
                    roadmap.Notes = request.Notes;

                roadmap.UpdatedAt = DateTime.UtcNow;

                _uow.StudentRoadmaps.Update(roadmap);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Roadmap updated successfully";

                _logger.LogInformation("Updated roadmap {RoadmapId}", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roadmap {RoadmapId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Roadmap update failed";
                return response;
            }
        }

        public async Task<StudentRoadmapResponse> DeleteRoadmapAsync(Guid id)
        {
            var response = new StudentRoadmapResponse { RoadmapId = id };

            try
            {
                var roadmap = await _uow.StudentRoadmaps.GetByIdAsync(id);
                if (roadmap == null)
                {
                    response.Errors.Add($"Roadmap with ID '{id}' not found");
                    response.Message = "Roadmap deletion failed";
                    return response;
                }

                _uow.StudentRoadmaps.Remove(roadmap);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Roadmap deleted successfully";

                _logger.LogInformation("Deleted roadmap {RoadmapId}", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting roadmap {RoadmapId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Roadmap deletion failed";
                return response;
            }
        }

        public async Task<StudentRoadmapResponse> CreateRoadmapFromTemplateAsync(
            Guid studentId,
            List<CreateStudentRoadmapRequest> roadmapItems)
        {
            var response = new StudentRoadmapResponse();

            try
            {
                // Validate student exists
                var student = await _uow.Students.GetByIdAsync(studentId);
                if (student == null)
                {
                    response.Errors.Add($"Student with ID '{studentId}' not found");
                    response.Message = "Bulk roadmap creation failed";
                    return response;
                }

                var createdCount = 0;
                var errors = new List<string>();

                foreach (var item in roadmapItems)
                {
                    item.StudentId = studentId; // Ensure correct student ID

                    // Check if already exists
                    var exists = await _uow.StudentRoadmaps.HasRoadmapEntryAsync(
             studentId,
                   item.SubjectId);

                    if (exists)
                    {
                        errors.Add($"Subject {item.SubjectId} already in roadmap - skipped");
                        continue;
                    }

                    var roadmap = new StudentRoadmap
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentId,
                        SubjectId = item.SubjectId,
                        SemesterId = item.SemesterId,
                        SequenceOrder = item.SequenceOrder,
                        Status = item.Status,
                        Notes = item.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _uow.StudentRoadmaps.AddAsync(roadmap);
                    createdCount++;
                }

                await _uow.SaveChangesAsync();

                response.Success = createdCount > 0;
                response.Message = $"Created {createdCount} roadmap entries" +
               (errors.Any() ? $". {errors.Count} items skipped." : "");
                response.Errors = errors;

                _logger.LogInformation(
             "Bulk created {Count} roadmap entries for student {StudentId}",
          createdCount, studentId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating roadmap for student {StudentId}", studentId);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Bulk roadmap creation failed";
                return response;
            }
        }

        // ==================== AUTOMATION ====================

        public async Task UpdateRoadmapOnEnrollmentAsync(Guid studentId, Guid subjectId)
        {
            try
            {
                await _uow.StudentRoadmaps.UpdateRoadmapStatusAsync(
           studentId,
            subjectId,
                "InProgress");

                await _uow.SaveChangesAsync();

                _logger.LogInformation(
          "Updated roadmap to InProgress for student {StudentId}, subject {SubjectId}",
           studentId, subjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                  "Error updating roadmap on enrollment for student {StudentId}, subject {SubjectId}",
         studentId, subjectId);
            }
        }

        public async Task UpdateRoadmapOnGradeAsync(
                 Guid studentId,
            Guid subjectId,
         decimal finalScore,
              string letterGrade)
        {
            try
            {
                var status = finalScore >= 5.0m ? "Completed" : "Failed"; // Assuming 5.0 is passing grade

                await _uow.StudentRoadmaps.UpdateRoadmapStatusAsync(
                      studentId,
                  subjectId,
                   status,
                 finalScore,
                letterGrade);

                await _uow.SaveChangesAsync();

                _logger.LogInformation(
               "Updated roadmap to {Status} for student {StudentId}, subject {SubjectId}",
             status, studentId, subjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                        "Error updating roadmap on grade for student {StudentId}, subject {SubjectId}",
                       studentId, subjectId);
            }
        }
    }
}
