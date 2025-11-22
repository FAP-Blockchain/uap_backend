using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.StudentRoadmap;
using Fap.Domain.Entities;
using Fap.Domain.Helpers;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
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
                var recommendations = new List<RecommendedSubjectDto>();

                // Load student with curriculum context to evaluate prerequisites
                var student = await _uow.Students.GetWithCurriculumAsync(studentId);
                if (student == null)
                {
                    _logger.LogWarning("Student {StudentId} not found when building recommendations", studentId);
                    return recommendations;
                }

                if (student.Curriculum == null || student.Curriculum.CurriculumSubjects == null ||
                    !student.Curriculum.CurriculumSubjects.Any())
                {
                    _logger.LogWarning("Student {StudentId} missing curriculum data for recommendations", studentId);
                    return recommendations;
                }

                var snapshot = CurriculumProgressHelper.BuildSnapshot(student);
                if (!snapshot.Subjects.Any())
                {
                    return recommendations;
                }

                var roadmapEntries = await _uow.StudentRoadmaps.GetStudentRoadmapAsync(studentId);
                var roadmapLookup = roadmapEntries.ToDictionary(r => r.SubjectId, r => r);

                var now = DateTime.UtcNow;
                var currentSemester = await _uow.Semesters.GetQueryable()
                    .Where(s => s.StartDate <= now && s.EndDate >= now)
                    .FirstOrDefaultAsync();

                var openSubjects = snapshot.Subjects.Values
                    .Where(s => s.Status == "Open")
                    .OrderBy(s => s.CurriculumSubject.SemesterNumber)
                    .ThenBy(s => s.CurriculumSubject.Subject.SubjectCode)
                    .Take(10)
                    .ToList();

                foreach (var subjectProgress in openSubjects)
                {
                    roadmapLookup.TryGetValue(subjectProgress.SubjectId, out var roadmap);
                    var subject = subjectProgress.CurriculumSubject.Subject;

                    // Preferred semester for class search: roadmap semester -> current semester -> null (all)
                    Guid? preferredSemesterId = roadmap?.SemesterId ?? currentSemester?.Id;

                    var classQuery = _uow.Classes.GetQueryable()
                        .Where(c => c.SubjectOffering.SubjectId == subjectProgress.SubjectId &&
                                    c.IsActive)
                        .Include(c => c.Teacher)
                            .ThenInclude(t => t.User)
                        .Include(c => c.Enrolls)
                        .Include(c => c.Slots)
                            .ThenInclude(s => s.TimeSlot);

                    List<Class> availableClasses;
                    if (preferredSemesterId.HasValue)
                    {
                        availableClasses = await classQuery
                            .Where(c => c.SubjectOffering.SemesterId == preferredSemesterId.Value)
                            .ToListAsync();

                        if (!availableClasses.Any())
                        {
                            availableClasses = await classQuery.ToListAsync();
                        }
                    }
                    else
                    {
                        availableClasses = await classQuery.ToListAsync();
                    }

                    var availableClassDtos = availableClasses.Select(c =>
                    {
                        var approvedCount = c.Enrolls?.Count(e => e.IsApproved) ?? 0;
                        return new AvailableClassInfoDto
                        {
                            ClassId = c.Id,
                            ClassCode = c.ClassCode,
                            TeacherName = c.Teacher?.User?.FullName ?? "TBA",
                            CurrentEnrollment = approvedCount,
                            MaxStudents = c.MaxEnrollment,
                            AvailableSlots = c.MaxEnrollment - approvedCount,
                            IsFull = approvedCount >= c.MaxEnrollment,
                            Schedule = GetClassSchedule(c.Slots)
                        };
                    }).ToList();

                    var reasonSegments = new List<string>();
                    if (roadmap != null)
                    {
                        reasonSegments.Add("Listed in roadmap");
                        if (!string.IsNullOrWhiteSpace(roadmap.Semester?.Name))
                        {
                            reasonSegments.Add($"Target semester {roadmap.Semester.Name}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(subjectProgress.PrerequisiteSubjectCode))
                    {
                        reasonSegments.Add($"Prerequisite {subjectProgress.PrerequisiteSubjectCode} completed");
                    }
                    else
                    {
                        reasonSegments.Add("No prerequisites required");
                    }

                    var recommendation = new RecommendedSubjectDto
                    {
                        SubjectId = subjectProgress.SubjectId,
                        SubjectCode = subject.SubjectCode,
                        SubjectName = subject.SubjectName,
                        Credits = subject.Credits,
                        SemesterId = roadmap?.SemesterId ?? Guid.Empty,
                        SemesterName = roadmap?.Semester?.Name ?? $"Semester {subjectProgress.CurriculumSubject.SemesterNumber}",
                        SequenceOrder = roadmap?.SequenceOrder ?? subjectProgress.CurriculumSubject.SemesterNumber * 10,
                        RecommendationReason = string.Join(" • ", reasonSegments),
                        Prerequisites = BuildPrerequisiteList(subject, subjectProgress),
                        AllPrerequisitesMet = subjectProgress.PrerequisitesMet,
                        HasAvailableClasses = availableClassDtos.Any(c => !c.IsFull),
                        AvailableClassCount = availableClassDtos.Count(c => !c.IsFull),
                        AvailableClasses = availableClassDtos
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

        private static List<string> BuildPrerequisiteList(Subject subject, SubjectProgressInfo progress)
        {
            var prereqs = new List<string>();

            if (!string.IsNullOrWhiteSpace(progress.PrerequisiteSubjectCode))
            {
                prereqs.Add(progress.PrerequisiteSubjectCode);
            }

            if (!string.IsNullOrWhiteSpace(subject.Prerequisites))
            {
                var codes = subject.Prerequisites
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(code => !string.IsNullOrWhiteSpace(code));

                prereqs.AddRange(codes);
            }

            return prereqs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Helper method to format class schedule from slots
        /// </summary>
        private string GetClassSchedule(ICollection<Slot>? slots)
        {
            if (slots == null || !slots.Any())
                return "Schedule TBA";

            var scheduleItems = slots
                .Where(s => s.TimeSlot != null)
                .OrderBy(s => s.Date)
                .GroupBy(s => s.Date.DayOfWeek)
                .Select(g => $"{g.Key} {g.First().TimeSlot!.StartTime:hh\\:mm}-{g.First().TimeSlot!.EndTime:hh\\:mm}")
                .ToList();

            return scheduleItems.Any() ? string.Join(", ", scheduleItems) : "Schedule TBA";
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

        public async Task<CurriculumRoadmapDto?> GetCurriculumRoadmapAsync(Guid studentId)
        {
            try
            {
                var student = await _uow.Students.GetWithCurriculumAsync(studentId);
                if (student == null)
                {
                    return null;
                }

                if (student.Curriculum == null || student.CurriculumId == null)
                {
                    _logger.LogWarning("Student {StudentId} does not have a curriculum assigned", studentId);
                    return null;
                }

                var snapshot = CurriculumProgressHelper.BuildSnapshot(student);

                var result = new CurriculumRoadmapDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    StudentName = student.User?.FullName ?? string.Empty,
                    CurriculumId = student.CurriculumId.Value,
                    CurriculumCode = student.Curriculum.Code,
                    CurriculumName = student.Curriculum.Name,
                    TotalSubjects = snapshot.TotalSubjects,
                    CompletedSubjects = snapshot.CompletedSubjects,
                    FailedSubjects = snapshot.FailedSubjects,
                    InProgressSubjects = snapshot.InProgressSubjects,
                    OpenSubjects = snapshot.OpenSubjects,
                    LockedSubjects = snapshot.LockedSubjects
                };

                if (!snapshot.CurriculumSubjects.Any())
                {
                    return result;
                }

                var semesterMap = new Dictionary<int, CurriculumSemesterDto>();

                foreach (var curriculumSubject in snapshot.CurriculumSubjects)
                {
                    if (!snapshot.Subjects.TryGetValue(curriculumSubject.SubjectId, out var progress))
                    {
                        continue;
                    }

                    if (!semesterMap.TryGetValue(curriculumSubject.SemesterNumber, out var semesterDto))
                    {
                        semesterDto = new CurriculumSemesterDto
                        {
                            SemesterNumber = curriculumSubject.SemesterNumber
                        };
                        semesterMap.Add(curriculumSubject.SemesterNumber, semesterDto);
                    }

                    semesterDto.Subjects.Add(MapToSubjectStatus(progress));
                }

                result.Semesters = semesterMap.Values
                    .OrderBy(s => s.SemesterNumber)
                    .Select(s =>
                    {
                        s.Subjects = s.Subjects
                            .OrderBy(sub => sub.SubjectCode)
                            .ToList();
                        return s;
                    })
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating curriculum roadmap for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<SubjectEligibilityResultDto> CheckCurriculumSubjectEligibilityAsync(Guid studentId, Guid subjectId)
        {
            try
            {
                var result = new SubjectEligibilityResultDto
                {
                    SubjectId = subjectId
                };

                var student = await _uow.Students.GetWithCurriculumAsync(studentId);
                if (student == null)
                {
                    result.IsEligible = false;
                    result.Reasons.Add("Student not found");
                    result.BlockingReason = result.Reasons.First();
                    return result;
                }

                Subject? subjectEntity = null;

                if (student.Curriculum?.CurriculumSubjects != null)
                {
                    subjectEntity = student.Curriculum.CurriculumSubjects
                        .FirstOrDefault(cs => cs.SubjectId == subjectId)?.Subject;
                }

                subjectEntity ??= await _uow.Subjects.GetByIdAsync(subjectId);

                if (subjectEntity != null)
                {
                    result.SubjectCode = subjectEntity.SubjectCode ?? string.Empty;
                    result.SubjectName = subjectEntity.SubjectName ?? string.Empty;
                }

                if (student.Curriculum == null || student.CurriculumId == null)
                {
                    var (legacyEligible, legacyReasons) = await CheckLegacyEligibilityAsync(studentId, subjectEntity);
                    result.HasCurriculumData = false;
                    result.SubjectInCurriculum = false;
                    result.IsEligible = legacyEligible;
                    result.Reasons = legacyReasons;
                    if (!legacyEligible && legacyReasons.Any())
                    {
                        result.BlockingReason = string.Join("; ", legacyReasons);
                    }
                    return result;
                }

                var snapshot = CurriculumProgressHelper.BuildSnapshot(student);
                result.HasCurriculumData = true;

                if (!snapshot.Subjects.TryGetValue(subjectId, out var progress) || progress == null)
                {
                    result.SubjectInCurriculum = false;
                    result.IsEligible = false;
                    result.Reasons.Add("Subject is not part of the student's curriculum");
                    result.BlockingReason = result.Reasons.First();
                    return result;
                }

                result.SubjectInCurriculum = true;
                result.CurrentStatus = progress.Status;
                result.PrerequisitesMet = progress.PrerequisitesMet;
                result.SubjectCode = progress.CurriculumSubject.Subject.SubjectCode ?? string.Empty;
                result.SubjectName = progress.CurriculumSubject.Subject.SubjectName ?? string.Empty;

                var reasons = new List<string>();

                if (!progress.PrerequisitesMet)
                {
                    reasons.Add(!string.IsNullOrEmpty(progress.PrerequisiteSubjectCode)
                        ? $"Missing prerequisite subject {progress.PrerequisiteSubjectCode}"
                        : "Missing prerequisite requirements");
                }

                if (progress.Status == "Completed")
                {
                    reasons.Add("Subject already completed");
                }

                if (progress.Status == "InProgress")
                {
                    reasons.Add("Subject is currently in progress");
                }

                if (progress.Status == "Locked" && !reasons.Any())
                {
                    reasons.Add("Subject is locked due to unmet prerequisites");
                }

                var isEligible = !reasons.Any();

                if (progress.Status == "Failed" && progress.PrerequisitesMet)
                {
                    isEligible = true;
                    reasons.Clear();
                }

                result.IsEligible = isEligible;
                result.Reasons = reasons;

                if (!isEligible && reasons.Any())
                {
                    result.BlockingReason = string.Join("; ", reasons);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking curriculum eligibility for student {StudentId} and subject {SubjectId}", studentId, subjectId);
                throw;
            }
        }

        public async Task<GraduationEligibilityDto> EvaluateGraduationEligibilityAsync(Guid studentId, bool persistIfEligible = false)
        {
            try
            {
                var student = await _uow.Students.GetWithCurriculumAsync(studentId);
                if (student == null)
                {
                    return new GraduationEligibilityDto
                    {
                        StudentId = studentId,
                        IsEligible = false,
                        Message = "Student not found"
                    };
                }

                var result = new GraduationEligibilityDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    StudentName = student.User?.FullName ?? string.Empty,
                    GraduationDate = student.GraduationDate
                };

                if (student.Curriculum == null || student.CurriculumId == null)
                {
                    result.IsEligible = false;
                    result.Message = "Student does not have an assigned curriculum";
                    return result;
                }

                var snapshot = CurriculumProgressHelper.BuildSnapshot(student);

                result.TotalSubjects = snapshot.TotalSubjects;
                result.CompletedSubjects = snapshot.CompletedSubjects;
                result.FailedSubjects = snapshot.FailedSubjects;
                result.InProgressSubjects = snapshot.InProgressSubjects;
                result.OpenSubjects = snapshot.OpenSubjects;
                result.LockedSubjects = snapshot.LockedSubjects;
                result.RequiredCredits = snapshot.RequiredCredits;
                result.CompletedCredits = snapshot.CompletedCredits;

                var outstandingSubjects = snapshot.Subjects.Values
                    .Where(sp => sp.Status != "Completed")
                    .OrderBy(sp => sp.CurriculumSubject.SemesterNumber)
                    .ThenBy(sp => sp.CurriculumSubject.Subject.SubjectCode)
                    .Select(MapToSubjectStatus)
                    .ToList();

                result.OutstandingSubjects = outstandingSubjects;
                result.IsEligible = !outstandingSubjects.Any();
                result.Message = result.IsEligible
                    ? "All curriculum subjects completed"
                    : "Outstanding subjects remain";

                if (result.IsEligible && persistIfEligible && !student.IsGraduated)
                {
                    student.IsGraduated = true;
                    student.GraduationDate = DateTime.UtcNow;
                    _uow.Students.Update(student);
                    await _uow.SaveChangesAsync();
                    result.GraduationDate = student.GraduationDate;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating graduation eligibility for student {StudentId}", studentId);
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
                    Status = string.IsNullOrWhiteSpace(request.Status)
                        ? "Planned"
                        : request.Status.Trim(),
                    Notes = request.Notes ?? string.Empty,
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
                        Status = string.IsNullOrWhiteSpace(item.Status)
                            ? "Planned"
                            : item.Status.Trim(),
                        Notes = item.Notes ?? string.Empty,
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

                var graduationStatus = await EvaluateGraduationEligibilityAsync(studentId, true);
                if (graduationStatus.IsEligible)
                {
                    _logger.LogInformation(
                        "Student {StudentId} now satisfies graduation requirements",
                        studentId);
                }

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

        private async Task<(bool IsEligible, List<string> Reasons)> CheckLegacyEligibilityAsync(Guid studentId, Subject? subject)
        {
            var reasons = new List<string>();

            if (subject == null)
            {
                reasons.Add("Subject not found");
                return (false, reasons);
            }

            if (string.IsNullOrWhiteSpace(subject.Prerequisites))
            {
                return (true, reasons);
            }

            var prerequisiteCodes = subject.Prerequisites
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(code => code.Trim())
                .Where(code => !string.IsNullOrEmpty(code))
                .ToList();

            if (!prerequisiteCodes.Any())
            {
                return (true, reasons);
            }

            var completedSubjects = await _uow.StudentRoadmaps.GetCompletedSubjectsAsync(studentId);
            var completedCodes = completedSubjects
                .Where(r => r.Subject != null && !string.IsNullOrWhiteSpace(r.Subject.SubjectCode))
                .Select(r => r.Subject.SubjectCode)
                .ToHashSet();

            var missingPrerequisites = prerequisiteCodes
                .Where(code => !completedCodes.Contains(code))
                .ToList();

            if (missingPrerequisites.Any())
            {
                reasons.Add($"Missing prerequisites: {string.Join(", ", missingPrerequisites)}");
                return (false, reasons);
            }

            return (true, reasons);
        }

        private static CurriculumSubjectStatusDto MapToSubjectStatus(SubjectProgressInfo progress)
        {
            var curriculumSubject = progress.CurriculumSubject;
            return new CurriculumSubjectStatusDto
            {
                SubjectId = progress.SubjectId,
                SubjectCode = curriculumSubject.Subject.SubjectCode ?? string.Empty,
                SubjectName = curriculumSubject.Subject.SubjectName ?? string.Empty,
                Credits = progress.Credits,
                Status = progress.Status,
                FinalScore = progress.FinalScore,
                CurrentClassId = progress.CurrentClassId,
                CurrentClassCode = progress.CurrentClassCode,
                CurrentSemesterId = progress.CurrentSemesterId,
                CurrentSemesterName = progress.CurrentSemesterName,
                PrerequisiteSubjectCode = progress.PrerequisiteSubjectCode,
                PrerequisitesMet = progress.PrerequisitesMet,
                Notes = progress.Status switch
                {
                    "Locked" when !string.IsNullOrEmpty(progress.PrerequisiteSubjectCode) =>
                        $"Requires {progress.PrerequisiteSubjectCode}",
                    "Failed" => "Needs retake",
                    _ => null
                }
            };
        }
    }
}
