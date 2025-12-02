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

                var candidateSubjects = snapshot.Subjects.Values
                    .Where(s => s.Status == "Open" || s.Status == "Failed")
                    .OrderBy(s => s.Status == "Failed" ? 0 : 1)
                    .ThenBy(s => s.CurriculumSubject.SemesterNumber)
                    .ThenBy(s => s.CurriculumSubject.Subject.SubjectCode)
                    .Take(12)
                    .ToList();

                foreach (var subjectProgress in candidateSubjects)
                {
                    roadmapLookup.TryGetValue(subjectProgress.SubjectId, out var roadmap);
                    var subject = subjectProgress.CurriculumSubject.Subject;
                    var isRetake = subjectProgress.Status == "Failed" || (roadmap?.Status == "Failed");

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
                    if (isRetake)
                    {
                        reasonSegments.Add("Previously failed - schedule a retake");
                    }

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
                        AvailableClasses = availableClassDtos,
                        IsRetake = isRetake,
                        RetakeReason = isRetake
                            ? (subjectProgress.FinalScore.HasValue
                                ? $"Last score {subjectProgress.FinalScore.Value:0.##}. Minimum 5.0 required"
                                : "Roadmap status marked as failed")
                            : null
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

        public async Task<List<RecommendedSubjectDto>> GetRetakeOptionsAsync(Guid studentId)
        {
            var recommended = await GetRecommendedSubjectsAsync(studentId);
            return recommended
                .Where(r => r.IsRetake)
                .OrderBy(r => r.SequenceOrder)
                .ToList();
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

                var calculatedGpa = CalculateCurriculumGpa(snapshot.Subjects.Values);
                result.CurrentGPA = calculatedGpa ?? (student.GPA > 0 ? student.GPA : (decimal?)null);

                if (!snapshot.CurriculumSubjects.Any())
                {
                    return result;
                }

                var allSemesters = await _uow.Semesters.GetQueryable()
                    .OrderBy(s => s.StartDate)
                    .ToListAsync();

                var semesterLookup = allSemesters.ToDictionary(s => s.Id, s => s);
                var finalSemesters = new List<CurriculumSemesterDto>();

                var subjectsWithActualSemester = snapshot.Subjects.Values
                    .Where(s => s.CurrentSemesterId.HasValue)
                    .GroupBy(s => s.CurrentSemesterId!.Value)
                    .Select(g =>
                    {
                        semesterLookup.TryGetValue(g.Key, out var semesterInfo);
                        var subjects = g.Select(MapToSubjectStatus)
                            .OrderBy(sub => sub.SubjectCode)
                            .ToList();

                        return new
                        {
                            SemesterId = g.Key,
                            SemesterName = semesterInfo?.Name ?? "Custom Semester",
                            StartDate = semesterInfo?.StartDate ?? DateTime.MaxValue,
                            Subjects = subjects
                        };
                    })
                    .OrderBy(g => g.StartDate)
                    .ThenBy(g => g.SemesterName)
                    .ToList();

                var sequenceNumber = 1;

                foreach (var group in subjectsWithActualSemester)
                {
                    finalSemesters.Add(new CurriculumSemesterDto
                    {
                        SemesterNumber = sequenceNumber++,
                        SemesterName = group.SemesterName,
                        Subjects = group.Subjects
                    });
                }

                var plannedSubjectGroups = snapshot.Subjects.Values
                    .Where(s => !s.CurrentSemesterId.HasValue)
                    .GroupBy(s => s.CurriculumSubject.SemesterNumber)
                    .OrderBy(g => g.Key);

                foreach (var group in plannedSubjectGroups)
                {
                    var subjects = group.Select(MapToSubjectStatus)
                        .OrderBy(sub => sub.SubjectCode)
                        .ToList();

                    finalSemesters.Add(new CurriculumSemesterDto
                    {
                        SemesterNumber = sequenceNumber++,
                        SemesterName = $"Planned Term {group.Key}",
                        Subjects = subjects
                    });
                }

                result.Semesters = finalSemesters;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating curriculum roadmap for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<CurriculumRoadmapSummaryDto?> GetCurriculumRoadmapSummaryAsync(Guid studentId)
        {
            try
            {
                var state = await BuildCurriculumRoadmapStateAsync(studentId);
                if (state == null)
                {
                    return null;
                }

                var summary = new CurriculumRoadmapSummaryDto
                {
                    StudentId = state.Student.Id,
                    StudentCode = state.Student.StudentCode,
                    StudentName = state.Student.StudentName,
                    CurriculumId = state.Student.CurriculumId,
                    CurriculumCode = state.Student.CurriculumCode,
                    CurriculumName = state.Student.CurriculumName,
                    CurrentGPA = state.CurrentGpa,
                    TotalSubjects = state.Subjects.Count,
                    CompletedSubjects = state.Subjects.Count(s => s.Subject.Status == "Completed"),
                    FailedSubjects = state.Subjects.Count(s => s.Subject.Status == "Failed"),
                    InProgressSubjects = state.Subjects.Count(s => s.Subject.Status == "InProgress"),
                    OpenSubjects = state.Subjects.Count(s => s.Subject.Status == "Open"),
                    LockedSubjects = state.Subjects.Count(s => s.Subject.Status == "Locked"),
                    SemesterSummaries = state.Subjects
                        .GroupBy(s => s.SemesterNumber)
                        .OrderBy(g => g.Key)
                        .Select(g => new CurriculumSemesterSummaryDto
                        {
                            SemesterNumber = g.Key,
                            SemesterName = $"Semester {g.Key}",
                            SubjectCount = g.Count(),
                            CompletedSubjects = g.Count(x => x.Subject.Status == "Completed"),
                            InProgressSubjects = g.Count(x => x.Subject.Status == "InProgress"),
                            PlannedSubjects = g.Count(x => x.Subject.Status == "Open"),
                            FailedSubjects = g.Count(x => x.Subject.Status == "Failed"),
                            LockedSubjects = g.Count(x => x.Subject.Status == "Locked")
                        })
                        .ToList()
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building curriculum roadmap summary for student {StudentId}", studentId);
                throw;
            }
        }

        public async Task<CurriculumSemesterDto?> GetCurriculumRoadmapSemesterAsync(Guid studentId, int semesterNumber)
        {
            try
            {
                if (semesterNumber <= 0)
                {
                    return null;
                }

                var state = await BuildCurriculumRoadmapStateAsync(studentId);
                if (state == null)
                {
                    return null;
                }

                var subjects = state.Subjects
                    .Where(s => s.SemesterNumber == semesterNumber)
                    .OrderBy(s => s.Subject.SubjectCode)
                    .Select(s => s.Subject)
                    .ToList();

                if (!subjects.Any())
                {
                    return null;
                }

                return new CurriculumSemesterDto
                {
                    SemesterNumber = semesterNumber,
                    SemesterName = $"Semester {semesterNumber}",
                    Subjects = subjects
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building curriculum roadmap semester {SemesterNumber} for student {StudentId}", semesterNumber, studentId);
                throw;
            }
        }

        private async Task<CurriculumRoadmapComputation?> BuildCurriculumRoadmapStateAsync(Guid studentId)
        {
            var studentRaw = await _uow.Students.GetQueryable()
                .AsNoTracking()
                .Where(s => s.Id == studentId)
                .Select(s => new
                {
                    s.Id,
                    s.StudentCode,
                    FullName = s.User.FullName,
                    s.CurriculumId,
                    CurriculumCode = s.Curriculum != null ? s.Curriculum.Code : null,
                    CurriculumName = s.Curriculum != null ? s.Curriculum.Name : null,
                    s.GPA
                })
                .FirstOrDefaultAsync();

            if (studentRaw == null)
            {
                return null;
            }

            if (!studentRaw.CurriculumId.HasValue)
            {
                _logger.LogWarning("Student {StudentId} is missing curriculum assignment", studentId);
                return null;
            }

            var studentSnapshot = new StudentRoadmapStudentSnapshot(
                studentRaw.Id,
                studentRaw.StudentCode,
                studentRaw.FullName ?? string.Empty,
                studentRaw.CurriculumId.Value,
                studentRaw.CurriculumCode ?? string.Empty,
                studentRaw.CurriculumName ?? string.Empty,
                studentRaw.GPA);

            var curriculumSubjects = await _uow.CurriculumSubjects.GetQueryable()
                .AsNoTracking()
                .Where(cs => cs.CurriculumId == studentSnapshot.CurriculumId)
                .Select(cs => new CurriculumSubjectDefinition
                {
                    SubjectId = cs.SubjectId,
                    SemesterNumber = cs.SemesterNumber,
                    SubjectCode = cs.Subject.SubjectCode,
                    SubjectName = cs.Subject.SubjectName,
                    Credits = cs.Subject.Credits,
                    PrerequisiteSubjectId = cs.PrerequisiteSubjectId,
                    PrerequisiteSubjectCode = cs.PrerequisiteSubject != null ? cs.PrerequisiteSubject.SubjectCode : null
                })
                .OrderBy(cs => cs.SemesterNumber)
                .ThenBy(cs => cs.SubjectCode)
                .ToListAsync();

            if (!curriculumSubjects.Any())
            {
                _logger.LogWarning("Curriculum {CurriculumId} has no subjects configured", studentSnapshot.CurriculumId);
                return null;
            }

            var gradeRows = await _uow.Grades.GetQueryable()
                .AsNoTracking()
                .Where(g => g.StudentId == studentId && g.Score.HasValue)
                .GroupBy(g => g.SubjectId)
                .Select(g => new GradeAggregateRow
                {
                    SubjectId = g.Key,
                    WeightedScore = g.Sum(x => (x.Score ?? 0m) * x.GradeComponent.WeightPercent),
                    TotalWeight = g.Sum(x => x.GradeComponent.WeightPercent),
                    LatestLetterGrade = g.Where(x => x.LetterGrade != null)
                        .OrderByDescending(x => x.UpdatedAt)
                        .Select(x => x.LetterGrade)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var gradeDict = gradeRows.ToDictionary(
                x => x.SubjectId,
                x => new GradeAggregateInfo(
                    x.TotalWeight > 0 ? Math.Round(x.WeightedScore / x.TotalWeight, 2) : (decimal?)null,
                    x.TotalWeight,
                    x.LatestLetterGrade));

            var attendanceRows = await _uow.Attendances.GetQueryable()
                .AsNoTracking()
                .Where(a => a.StudentId == studentId)
                .GroupBy(a => a.SubjectId)
                .Select(g => new AttendanceAggregateRow
                {
                    SubjectId = g.Key,
                    TotalSessions = g.Count(),
                    PresentSessions = g.Count(a => a.IsPresent || a.IsExcused)
                })
                .ToListAsync();

            var attendanceDict = attendanceRows.ToDictionary(
                x => x.SubjectId,
                x => CreateAttendanceAggregate(x.PresentSessions, x.TotalSessions));

            var enrollmentRows = await _uow.Enrolls.GetQueryable()
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsApproved)
                .Select(e => new EnrollmentAggregateRow
                {
                    SubjectId = e.Class.SubjectOffering.SubjectId,
                    ClassId = e.ClassId,
                    ClassCode = e.Class.ClassCode,
                    SemesterId = e.Class.SubjectOffering.SemesterId,
                    SemesterName = e.Class.SubjectOffering.Semester.Name,
                    SemesterStartDate = e.Class.SubjectOffering.Semester.StartDate,
                    SemesterEndDate = e.Class.SubjectOffering.Semester.EndDate,
                    RegisteredAt = e.RegisteredAt
                })
                .ToListAsync();

            var enrollmentDict = enrollmentRows
                .GroupBy(x => x.SubjectId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.RegisteredAt).First());

            var completedSubjectIds = gradeDict
                .Where(kvp => kvp.Value.TotalWeight >= 100 && kvp.Value.FinalScore.HasValue && kvp.Value.FinalScore.Value >= 5m)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            var failedSubjectIds = gradeDict
                .Where(kvp => kvp.Value.TotalWeight >= 100 && kvp.Value.FinalScore.HasValue && kvp.Value.FinalScore.Value < 5m)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            var now = DateTime.UtcNow;
            var planItems = new List<CurriculumSubjectPlanItem>();

            foreach (var definition in curriculumSubjects)
            {
                gradeDict.TryGetValue(definition.SubjectId, out var gradeInfo);
                attendanceDict.TryGetValue(definition.SubjectId, out var attendanceInfo);
                enrollmentDict.TryGetValue(definition.SubjectId, out var enrollmentInfo);

                var prerequisitesMet = definition.PrerequisiteSubjectId == null ||
                    completedSubjectIds.Contains(definition.PrerequisiteSubjectId.Value);

                var attendanceRequirementMet = attendanceInfo?.RequirementMet ?? false;
                var status = DetermineSubjectStatus(
                    definition.SubjectId,
                    completedSubjectIds,
                    failedSubjectIds,
                    IsCurrentEnrollment(enrollmentInfo, now),
                    enrollmentInfo != null,
                    prerequisitesMet,
                    attendanceRequirementMet,
                    gradeInfo?.FinalScore,
                    gradeInfo?.TotalWeight ?? 0);

                var statusDto = new CurriculumSubjectStatusDto
                {
                    SubjectId = definition.SubjectId,
                    SubjectCode = definition.SubjectCode,
                    SubjectName = definition.SubjectName,
                    Credits = definition.Credits,
                    Status = status,
                    FinalScore = gradeInfo?.FinalScore,
                    CurrentClassId = enrollmentInfo?.ClassId,
                    CurrentClassCode = enrollmentInfo?.ClassCode,
                    CurrentSemesterId = enrollmentInfo?.SemesterId,
                    CurrentSemesterName = enrollmentInfo?.SemesterName,
                    PrerequisiteSubjectCode = definition.PrerequisiteSubjectCode,
                    PrerequisitesMet = prerequisitesMet,
                    AttendancePercentage = attendanceInfo?.Percentage,
                    AttendanceRequirementMet = attendanceRequirementMet,
                    Notes = status switch
                    {
                        "Locked" when !string.IsNullOrEmpty(definition.PrerequisiteSubjectCode) =>
                            $"Requires {definition.PrerequisiteSubjectCode}",
                        "Failed" => "Needs retake",
                        _ => null
                    }
                };

                planItems.Add(new CurriculumSubjectPlanItem
                {
                    SemesterNumber = definition.SemesterNumber,
                    Credits = definition.Credits,
                    Subject = statusDto
                });
            }

            var calculatedGpa = CalculateGpaFromSubjects(planItems);
            var gpa = calculatedGpa ?? (studentSnapshot.Gpa > 0 ? studentSnapshot.Gpa : (decimal?)null);

            return new CurriculumRoadmapComputation
            {
                Student = studentSnapshot,
                Subjects = planItems,
                CurrentGpa = gpa
            };
        }

        private static AttendanceAggregateInfo CreateAttendanceAggregate(int presentSessions, int totalSessions)
        {
            if (totalSessions == 0)
            {
                return new AttendanceAggregateInfo(null, false, false);
            }

            var percentage = Math.Round((decimal)presentSessions / totalSessions * 100m, 2);
            return new AttendanceAggregateInfo(percentage, percentage >= 80m, true);
        }

        private static decimal? CalculateGpaFromSubjects(IEnumerable<CurriculumSubjectPlanItem> items)
        {
            decimal totalGradePoints = 0m;
            int totalCredits = 0;

            foreach (var item in items)
            {
                if (!item.Subject.FinalScore.HasValue || item.Credits <= 0)
                {
                    continue;
                }

                var letterGrade = GradeHelper.CalculateLetterGrade(item.Subject.FinalScore.Value);
                var gradePoint = GradeHelper.GetGradePoint(letterGrade);

                totalGradePoints += gradePoint * item.Credits;
                totalCredits += item.Credits;
            }

            if (totalCredits == 0)
            {
                return null;
            }

            return Math.Round(totalGradePoints / totalCredits, 2);
        }

        private static bool IsCurrentEnrollment(EnrollmentAggregateRow? enrollment, DateTime now)
        {
            if (enrollment == null)
            {
                return false;
            }

            if (!enrollment.SemesterStartDate.HasValue || !enrollment.SemesterEndDate.HasValue)
            {
                return true;
            }

            if (now >= enrollment.SemesterStartDate.Value && now <= enrollment.SemesterEndDate.Value)
            {
                return true;
            }

            if (now < enrollment.SemesterStartDate.Value)
            {
                return true;
            }

            return false;
        }

        private static string DetermineSubjectStatus(
            Guid subjectId,
            HashSet<Guid> completedSubjectIds,
            HashSet<Guid> failedSubjectIds,
            bool isCurrentEnrollment,
            bool hasApprovedEnrollment,
            bool prerequisitesMet,
            bool attendanceRequirementMet,
            decimal? finalScore,
            int totalWeight)
        {
            if (completedSubjectIds.Contains(subjectId))
            {
                return attendanceRequirementMet ? "Completed" : "InProgress";
            }

            if (failedSubjectIds.Contains(subjectId))
            {
                return "Failed";
            }

            if (isCurrentEnrollment || hasApprovedEnrollment)
            {
                return "InProgress";
            }

            if (!prerequisitesMet)
            {
                return "Locked";
            }

            if (finalScore.HasValue && totalWeight > 0)
            {
                return "InProgress";
            }

            return "Open";
        }

        public async Task<SubjectEligibilityResultDto> CheckCurriculumSubjectEligibilityAsync(Guid studentId, Guid subjectId)
        {
            try
            {
                var result = new SubjectEligibilityResultDto
                {
                    SubjectId = subjectId
                };

                var state = await BuildCurriculumRoadmapStateAsync(studentId);
                if (state == null)
                {
                    var subjectEntity = await _uow.Subjects.GetByIdAsync(subjectId);
                    if (subjectEntity != null)
                    {
                        result.SubjectCode = subjectEntity.SubjectCode ?? string.Empty;
                        result.SubjectName = subjectEntity.SubjectName ?? string.Empty;
                    }

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

                result.HasCurriculumData = true;

                var planItem = state.Subjects
                    .FirstOrDefault(s => s.Subject.SubjectId == subjectId);

                if (planItem == null)
                {
                    var subjectEntity = await _uow.Subjects.GetByIdAsync(subjectId);
                    result.SubjectInCurriculum = false;
                    result.IsEligible = false;
                    result.SubjectCode = subjectEntity?.SubjectCode ?? string.Empty;
                    result.SubjectName = subjectEntity?.SubjectName ?? string.Empty;
                    result.Reasons.Add("Subject is not part of the student's curriculum");
                    result.BlockingReason = result.Reasons.First();
                    return result;
                }

                var subjectStatus = planItem.Subject;
                result.SubjectInCurriculum = true;
                result.SubjectCode = subjectStatus.SubjectCode;
                result.SubjectName = subjectStatus.SubjectName;
                result.CurrentStatus = subjectStatus.Status;
                result.PrerequisitesMet = subjectStatus.PrerequisitesMet;

                var reasons = new List<string>();

                if (!subjectStatus.PrerequisitesMet)
                {
                    reasons.Add(!string.IsNullOrEmpty(subjectStatus.PrerequisiteSubjectCode)
                        ? $"Missing prerequisite subject {subjectStatus.PrerequisiteSubjectCode}"
                        : "Missing prerequisite requirements");
                }

                if (subjectStatus.Status == "Completed")
                {
                    reasons.Add("Subject already completed");
                }

                if (subjectStatus.Status == "InProgress")
                {
                    reasons.Add("Subject is currently in progress");
                }

                if (subjectStatus.Status == "Locked" && !reasons.Any())
                {
                    reasons.Add("Subject is locked due to unmet prerequisites");
                }

                var isEligible = !reasons.Any();

                if (subjectStatus.Status == "Failed" && subjectStatus.PrerequisitesMet)
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

        public async Task<GraduationEligibilityDto> EvaluateGraduationEligibilityAsync(
            Guid studentId,
            bool persistIfEligible = false)
        {
            var state = await BuildCurriculumRoadmapStateAsync(studentId);
            if (state == null)
            {
                return new GraduationEligibilityDto
                {
                    StudentId = studentId,
                    IsEligible = false,
                    Message = "Curriculum data not found."
                };
            }

            // Basic counts
            var subjects = state.Subjects;
            var completed = subjects.Where(s => s.Subject.Status == "Completed").ToList();

            var result = new GraduationEligibilityDto
            {
                StudentId = studentId,
                StudentName = state.Student.StudentName,
                StudentCode = state.Student.StudentCode,
                CurriculumName = state.Student.CurriculumName,

                TotalSubjects = subjects.Count,
                CompletedSubjects = completed.Count,
                InProgressSubjects = subjects.Count(s => s.Subject.Status == "InProgress"),
                OpenSubjects = subjects.Count(s => s.Subject.Status == "Open"),
                LockedSubjects = subjects.Count(s => s.Subject.Status == "Locked"),

                RequiredCredits = subjects.Sum(s => s.Credits),
                CompletedCredits = completed.Sum(s => s.Credits),

                OutstandingSubjects = subjects
                    .Where(s => s.Subject.Status != "Completed")
                    .OrderBy(s => s.SemesterNumber)
                    .ThenBy(s => s.Subject.SubjectCode)
                    .Select(s => s.Subject)
                    .ToList()
            };

            result.IsEligible = result.OutstandingSubjects.Count == 0;
            result.Message = result.IsEligible
                ? "All curriculum subjects completed."
                : "Outstanding subjects remain.";

            // Only load tracked student when you actually need to update DB
            if (persistIfEligible && result.IsEligible)
            {
                var tracked = await _uow.Students.GetByIdAsync(studentId);
                if (tracked != null)
                {
                    tracked.IsGraduated = true;
                    tracked.GraduationDate = DateTime.UtcNow;

                    await _uow.SaveChangesAsync();
                }
            }

            return result;
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

        public async Task<StudentRoadmapResponse> PlanRetakeAsync(Guid studentId, Guid roadmapId, PlanRetakeRequest request)
        {
            var response = new StudentRoadmapResponse { RoadmapId = roadmapId };

            try
            {
                if (request == null)
                {
                    response.Errors.Add("Request payload is required");
                    response.Message = "Retake planning failed";
                    return response;
                }

                var roadmap = await _uow.StudentRoadmaps.GetByIdAsync(roadmapId);
                if (roadmap == null || roadmap.StudentId != studentId)
                {
                    response.Errors.Add("Roadmap entry not found for this student");
                    response.Message = "Retake planning failed";
                    return response;
                }

                if (!string.Equals(roadmap.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add("Only failed subjects can be scheduled for retake");
                    response.Message = "Retake planning failed";
                    return response;
                }

                var semester = await _uow.Semesters.GetByIdAsync(request.SemesterId);
                if (semester == null)
                {
                    response.Errors.Add("Target semester not found");
                    response.Message = "Retake planning failed";
                    return response;
                }

                if (semester.EndDate < DateTime.UtcNow)
                {
                    response.Errors.Add("Cannot plan retake for a semester that has already ended");
                    response.Message = "Retake planning failed";
                    return response;
                }

                roadmap.SemesterId = semester.Id;
                roadmap.Status = "Planned";
                roadmap.StartedAt = null;
                roadmap.CompletedAt = null;
                roadmap.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    var prefix = string.IsNullOrWhiteSpace(roadmap.Notes) ? string.Empty : roadmap.Notes + Environment.NewLine;
                    roadmap.Notes = prefix + "[Retake] " + request.Notes.Trim();
                }

                _uow.StudentRoadmaps.Update(roadmap);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Retake scheduled for {semester.Name}";

                _logger.LogInformation(
                    "Student {StudentId} planned a retake for roadmap {RoadmapId} in semester {SemesterId}",
                    studentId,
                    roadmapId,
                    semester.Id);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error planning retake for roadmap {RoadmapId}", roadmapId);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Retake planning failed";
                return response;
            }
        }

        // ==================== AUTOMATION ====================

        public async Task UpdateRoadmapOnEnrollmentAsync(Guid studentId, Guid subjectId)
        {
            try
            {
                // This method is called from EnrollmentService with only studentId and subjectId
                // We need to get the actual semester from the enrolled class
                // For now, just update status - the semesterId will be updated in ApproveEnrollmentAsync
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

        /// <summary>
        /// Update roadmap with actual semester when student enrolls - called with semesterId
        /// </summary>
        public async Task UpdateRoadmapWithActualSemesterAsync(Guid studentId, Guid subjectId, Guid actualSemesterId)
        {
            try
            {
                await _uow.StudentRoadmaps.UpdateRoadmapOnEnrollmentAsync(
                    studentId,
                    subjectId,
                    actualSemesterId);

                await _uow.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated roadmap for student {StudentId}, subject {SubjectId} to semester {SemesterId} with status InProgress",
                    studentId, subjectId, actualSemesterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating roadmap semester for student {StudentId}, subject {SubjectId}",
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

        private static decimal? CalculateCurriculumGpa(IEnumerable<SubjectProgressInfo> subjects)
        {
            decimal totalGradePoints = 0m;
            int totalCreditsWithGrades = 0;

            foreach (var subject in subjects)
            {
                if (!subject.FinalScore.HasValue || subject.Credits <= 0)
                {
                    continue;
                }

                var letterGrade = GradeHelper.CalculateLetterGrade(subject.FinalScore.Value);
                var gradePoint = GradeHelper.GetGradePoint(letterGrade);

                totalGradePoints += gradePoint * subject.Credits;
                totalCreditsWithGrades += subject.Credits;
            }

            if (totalCreditsWithGrades == 0)
            {
                return null;
            }

            return Math.Round(totalGradePoints / totalCreditsWithGrades, 2);
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
                AttendancePercentage = progress.AttendancePercentage,
                AttendanceRequirementMet = progress.AttendanceRequirementMet,
                Notes = progress.Status switch
                {
                    "Locked" when !string.IsNullOrEmpty(progress.PrerequisiteSubjectCode) =>
                        $"Requires {progress.PrerequisiteSubjectCode}",
                    "Failed" => "Needs retake",
                    _ => null
                }
            };
        }

        private sealed record StudentRoadmapStudentSnapshot(
            Guid Id,
            string StudentCode,
            string StudentName,
            int CurriculumId,
            string CurriculumCode,
            string CurriculumName,
            decimal Gpa);

        private sealed class CurriculumSubjectDefinition
        {
            public Guid SubjectId { get; set; }
            public int SemesterNumber { get; set; }
            public string SubjectCode { get; set; } = string.Empty;
            public string SubjectName { get; set; } = string.Empty;
            public int Credits { get; set; }
            public Guid? PrerequisiteSubjectId { get; set; }
            public string? PrerequisiteSubjectCode { get; set; }
        }

        private sealed record GradeAggregateRow
        {
            public Guid SubjectId { get; init; }
            public decimal WeightedScore { get; init; }
            public int TotalWeight { get; init; }
            public string? LatestLetterGrade { get; init; }
        }

        private sealed record GradeAggregateInfo(decimal? FinalScore, int TotalWeight, string? LetterGrade);

        private sealed record AttendanceAggregateRow
        {
            public Guid SubjectId { get; init; }
            public int TotalSessions { get; init; }
            public int PresentSessions { get; init; }
        }

        private sealed record AttendanceAggregateInfo(decimal? Percentage, bool RequirementMet, bool HasRecords);

        private sealed record EnrollmentAggregateRow
        {
            public Guid SubjectId { get; init; }
            public Guid ClassId { get; init; }
            public string ClassCode { get; init; } = string.Empty;
            public Guid? SemesterId { get; init; }
            public string? SemesterName { get; init; }
            public DateTime? SemesterStartDate { get; init; }
            public DateTime? SemesterEndDate { get; init; }
            public DateTime RegisteredAt { get; init; }
        }

        private sealed class CurriculumSubjectPlanItem
        {
            public int SemesterNumber { get; init; }
            public int Credits { get; init; }
            public CurriculumSubjectStatusDto Subject { get; init; } = null!;
        }

        private sealed class CurriculumRoadmapComputation
        {
            public StudentRoadmapStudentSnapshot Student { get; init; } = null!;
            public List<CurriculumSubjectPlanItem> Subjects { get; init; } = new();
            public decimal? CurrentGpa { get; init; }
        }
    }
}
