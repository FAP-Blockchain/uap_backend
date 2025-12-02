using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.Entities;
using Fap.Domain.Helpers;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class GradeService : IGradeService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GradeService> _logger;

        public GradeService(IUnitOfWork uow, IMapper mapper, ILogger<GradeService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<GradeResponse> CreateGradeAsync(CreateGradeRequest request)
        {
            var response = new GradeResponse();

            try
            {
                // Validate student exists
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null)
                {
                    response.Errors.Add($"Student with ID '{request.StudentId}' not found");
                    response.Message = "Grade creation failed";
                    return response;
                }

                // Validate subject exists
                var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    response.Errors.Add($"Subject with ID '{request.SubjectId}' not found");
                    response.Message = "Grade creation failed";
                    return response;
                }

                // Validate grade component exists
                var gradeComponent = await _uow.GradeComponents.GetByIdAsync(request.GradeComponentId);
                if (gradeComponent == null)
                {
                    response.Errors.Add($"Grade component with ID '{request.GradeComponentId}' not found");
                    response.Message = "Grade creation failed";
                    return response;
                }

                // Check if grade already exists for this combination
                var existingGrade = await _uow.Grades.GetGradeByStudentSubjectComponentAsync(
                    request.StudentId, request.SubjectId, request.GradeComponentId);

                if (existingGrade != null)
                {
                    response.Errors.Add("Grade already exists for this student, subject, and component combination");
                    response.Message = "Grade creation failed";
                    return response;
                }

                // Auto-calculate letter grade based on score
                var letterGrade = GradeHelper.CalculateLetterGrade(request.Score);

                var newGrade = new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    SubjectId = request.SubjectId,
                    GradeComponentId = request.GradeComponentId,
                    Score = request.Score,
                    LetterGrade = letterGrade,
                    UpdatedAt = DateTime.UtcNow
                };

                await _uow.Grades.AddAsync(newGrade);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade created successfully";
                response.GradeId = newGrade.Id;

                _logger.LogInformation("Grade created for student {StudentId} in subject {SubjectId}",
                    request.StudentId, request.SubjectId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating grade");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Grade creation failed";
                return response;
            }
        }

        public async Task<BulkGradeResponse> CreateGradesAsync(BulkCreateGradesRequest request)
        {
            var response = new BulkGradeResponse
            {
                TotalProcessed = request.Grades.Count
            };

            try
            {
                // Validate and prepare grades
                var gradesToCreate = new List<Grade>();
                var validationErrors = new List<string>();

                // Get all student IDs, subject IDs, and component IDs for batch validation
                var studentIds = request.Grades.Select(g => g.StudentId).Distinct().ToList();
                var subjectIds = request.Grades.Select(g => g.SubjectId).Distinct().ToList();
                var componentIds = request.Grades.Select(g => g.GradeComponentId).Distinct().ToList();

                // Batch load entities
                var students = await _uow.Students.GetByIdsAsync(studentIds);
                var subjects = await _uow.Subjects.GetByIdsAsync(subjectIds);
                var gradeComponents = await _uow.GradeComponents.GetByIdsAsync(componentIds);

                // Create dictionaries for fast lookup
                var studentDict = students.ToDictionary(s => s.Id);
                var subjectDict = subjects.ToDictionary(s => s.Id);
                var componentDict = gradeComponents.ToDictionary(gc => gc.Id);

                for (int i = 0; i < request.Grades.Count; i++)
                {
                    var gradeRequest = request.Grades[i];
                    var index = i + 1;

                    // Validate student exists
                    if (!studentDict.ContainsKey(gradeRequest.StudentId))
                    {
                        validationErrors.Add($"Grade {index}: Student with ID '{gradeRequest.StudentId}' not found");
                        continue;
                    }

                    // Validate subject exists
                    if (!subjectDict.ContainsKey(gradeRequest.SubjectId))
                    {
                        validationErrors.Add($"Grade {index}: Subject with ID '{gradeRequest.SubjectId}' not found");
                        continue;
                    }

                    // Validate grade component exists
                    if (!componentDict.ContainsKey(gradeRequest.GradeComponentId))
                    {
                        validationErrors.Add($"Grade {index}: Grade component with ID '{gradeRequest.GradeComponentId}' not found");
                        continue;
                    }

                    // Check if grade already exists for this combination
                    var existingGrade = await _uow.Grades.GetGradeByStudentSubjectComponentAsync(
                        gradeRequest.StudentId, gradeRequest.SubjectId, gradeRequest.GradeComponentId);

                    if (existingGrade != null)
                    {
                        validationErrors.Add($"Grade {index}: Grade already exists for student {studentDict[gradeRequest.StudentId].StudentCode}, subject {subjectDict[gradeRequest.SubjectId].SubjectCode}, component {componentDict[gradeRequest.GradeComponentId].Name}");
                        continue;
                    }

                    // Auto-calculate letter grade
                    var letterGrade = GradeHelper.CalculateLetterGrade(gradeRequest.Score);

                    var newGrade = new Grade
                    {
                        Id = Guid.NewGuid(),
                        StudentId = gradeRequest.StudentId,
                        SubjectId = gradeRequest.SubjectId,
                        GradeComponentId = gradeRequest.GradeComponentId,
                        Score = gradeRequest.Score,
                        LetterGrade = letterGrade,
                        UpdatedAt = DateTime.UtcNow
                    };

                    gradesToCreate.Add(newGrade);
                    response.CreatedGradeIds.Add(newGrade.Id);
                }

                // Save all valid grades
                if (gradesToCreate.Any())
                {
                    await _uow.Grades.AddRangeAsync(gradesToCreate);
                    await _uow.SaveChangesAsync();

                    response.SuccessCount = gradesToCreate.Count;
                    _logger.LogInformation("Successfully created {Count} grades", gradesToCreate.Count);
                }

                // Set response details
                response.FailedCount = validationErrors.Count;
                response.Errors = validationErrors;
                response.Success = response.SuccessCount > 0;
                
                if (response.Success && response.FailedCount == 0)
                {
                    response.Message = $"Successfully created all {response.SuccessCount} grades";
                }
                else if (response.Success && response.FailedCount > 0)
                {
                    response.Message = $"Partially successful: {response.SuccessCount} grades created, {response.FailedCount} failed";
                }
                else
                {
                    response.Message = "Failed to create any grades";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk grades");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Bulk grade creation failed";
                response.Success = false;
                return response;
            }
        }

        public async Task<GradeDetailDto?> GetGradeByIdAsync(Guid id)
        {
            try
            {
                var grade = await _uow.Grades.GetByIdWithDetailsAsync(id);
                if (grade == null)
                    return null;

                return new GradeDetailDto
                {
                    Id = grade.Id,
                    Score = grade.Score ?? 0,
                    LetterGrade = grade.LetterGrade ?? string.Empty,
                    UpdatedAt = grade.UpdatedAt,
                    StudentId = grade.StudentId,
                    StudentCode = grade.Student.StudentCode,
                    StudentName = grade.Student.User.FullName,
                    StudentEmail = grade.Student.User.Email,
                    SubjectId = grade.SubjectId,
                    SubjectCode = grade.Subject.SubjectCode,
                    SubjectName = grade.Subject.SubjectName,
                    Credits = grade.Subject.Credits,
                    GradeComponentId = grade.GradeComponentId,
                    ComponentName = grade.GradeComponent.Name,
                    ComponentWeight = grade.GradeComponent.WeightPercent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grade {GradeId}", id);
                return null;
            }
        }

        public async Task<GradeResponse> UpdateGradeAsync(Guid id, UpdateGradeRequest request)
        {
            var response = new GradeResponse();

            try
            {
                var grade = await _uow.Grades.GetByIdAsync(id);
                if (grade == null)
                {
                    response.Errors.Add($"Grade with ID '{id}' not found");
                    response.Message = "Grade update failed";
                    return response;
                }

                // Update score (can be null to clear the score)
                grade.Score = request.Score;
                
                // Auto-calculate letter grade only if score is provided
                grade.LetterGrade = request.Score.HasValue 
                    ? GradeHelper.CalculateLetterGrade(request.Score.Value)
                    : null;
                    
                grade.UpdatedAt = DateTime.UtcNow;

                _uow.Grades.Update(grade);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = request.Score.HasValue 
                    ? "Grade updated successfully"
                    : "Grade cleared successfully";
                response.GradeId = grade.Id;

                _logger.LogInformation("Grade {GradeId} updated. Score: {Score}", id, request.Score?.ToString() ?? "null");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grade {GradeId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Grade update failed";
                return response;
            }
        }

        public async Task<BulkGradeResponse> UpdateGradesAsync(BulkUpdateGradesRequest request)
        {
            var response = new BulkGradeResponse
            {
                TotalProcessed = request.Grades.Count
            };

            try
            {
                var validationErrors = new List<string>();
                var gradesToUpdate = new List<Grade>();

                // Get all grade IDs for batch loading
                var gradeIds = request.Grades.Select(g => g.GradeId).Distinct().ToList();

                // Batch load grades
                var existingGrades = await _uow.Grades.GetByIdsAsync(gradeIds);
                var gradeDict = existingGrades.ToDictionary(g => g.Id);

                for (int i = 0; i < request.Grades.Count; i++)
                {
                    var gradeUpdate = request.Grades[i];
                    var index = i + 1;

                    // Validate grade exists
                    if (!gradeDict.ContainsKey(gradeUpdate.GradeId))
                    {
                        validationErrors.Add($"Grade {index}: Grade with ID '{gradeUpdate.GradeId}' not found");
                        continue;
                    }

                    var grade = gradeDict[gradeUpdate.GradeId];

                    // Update score (can be null to clear the score)
                    grade.Score = gradeUpdate.Score;
                    
                    // Auto-calculate letter grade only if score is provided
                    grade.LetterGrade = gradeUpdate.Score.HasValue 
                        ? GradeHelper.CalculateLetterGrade(gradeUpdate.Score.Value)
                        : null;
                        
                    grade.UpdatedAt = DateTime.UtcNow;

                    gradesToUpdate.Add(grade);
                    response.UpdatedGradeIds.Add(grade.Id);
                }

                // Update all valid grades
                if (gradesToUpdate.Any())
                {
                    foreach (var grade in gradesToUpdate)
                    {
                        _uow.Grades.Update(grade);
                    }
                    await _uow.SaveChangesAsync();

                    response.SuccessCount = gradesToUpdate.Count;
                    _logger.LogInformation("Successfully updated {Count} grades", gradesToUpdate.Count);
                }

                // Set response details
                response.FailedCount = validationErrors.Count;
                response.Errors = validationErrors;
                response.Success = response.SuccessCount > 0;

                if (response.Success && response.FailedCount == 0)
                {
                    response.Message = $"Successfully updated all {response.SuccessCount} grades";
                }
                else if (response.Success && response.FailedCount > 0)
                {
                    response.Message = $"Partially successful: {response.SuccessCount} grades updated, {response.FailedCount} failed";
                }
                else
                {
                    response.Message = "Failed to update any grades";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bulk grades");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Bulk grade update failed";
                response.Success = false;
                return response;
            }
        }

        public async Task<ClassGradeReportDto?> GetClassGradesAsync(Guid classId, GetClassGradesRequest request)
        {
            try
            {
                // Get class with subject and teacher
                var classEntity = await _uow.Classes.GetByIdWithDetailsAsync(classId);
                if (classEntity == null)
                    return null;

                // Get all students in class
                var classMembers = await _uow.Classes.GetClassRosterAsync(classId);
                var studentIds = classMembers.Select(cm => cm.StudentId).ToList();

                // Get all grades for this class's subject
                var grades = await _uow.Grades.GetGradesByClassIdAsync(classId);

                // Get all grade components
                var gradeComponents = (await _uow.GradeComponents.GetAllAsync()).ToList();

                var report = new ClassGradeReportDto
                {
                    ClassId = classEntity.Id,
                    ClassCode = classEntity.ClassCode,
                    SubjectCode = classEntity.SubjectOffering.Subject.SubjectCode,
                    SubjectName = classEntity.SubjectOffering.Subject.SubjectName,
                    TeacherName = classEntity.Teacher?.User?.FullName ?? "N/A",
                    Students = new List<StudentGradeInClassDto>()
                };

                foreach (var member in classMembers)
                {
                    var studentGrades = grades.Where(g => g.StudentId == member.StudentId).ToList();

                    var componentGrades = gradeComponents.Select(gc =>
                    {
                        var grade = studentGrades.FirstOrDefault(g => g.GradeComponentId == gc.Id);
                        return new ComponentGradeDto
                        {
                            GradeId = grade?.Id,
                            GradeComponentId = gc.Id,
                            ComponentName = gc.Name,
                            ComponentWeight = gc.WeightPercent,
                            Score = grade?.Score,
                            LetterGrade = grade?.LetterGrade ?? string.Empty
                        };
                    }).ToList();

                    // Filter by component if requested
                    if (request.GradeComponentId.HasValue)
                    {
                        componentGrades = componentGrades
                            .Where(cg => cg.GradeComponentId == request.GradeComponentId.Value)
                            .ToList();
                    }

                    // Calculate average
                    var gradesWithScores = componentGrades.Where(cg => cg.Score.HasValue).ToList();
                    decimal? averageScore = null;
                    string finalLetterGrade = string.Empty;

                    if (gradesWithScores.Any())
                    {
                        decimal totalWeightedScore = 0;
                        int totalWeight = 0;

                        foreach (var cg in gradesWithScores)
                        {
                            // Skip Attendance component in final score calculation
                            if (cg.ComponentName.Contains("Attendance", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (cg.Score.HasValue)
                            {
                                totalWeightedScore += cg.Score.Value * cg.ComponentWeight;
                                totalWeight += cg.ComponentWeight;
                            }
                        }

                        if (totalWeight > 0)
                        {
                            averageScore = totalWeightedScore / totalWeight;
                            if (averageScore.HasValue)
                            {
                                finalLetterGrade = GradeHelper.CalculateLetterGrade(averageScore.Value);
                            }
                        }
                    }

                    report.Students.Add(new StudentGradeInClassDto
                    {
                        StudentId = member.StudentId,
                        StudentCode = member.Student.StudentCode,
                        StudentName = member.Student.User.FullName,
                        Grades = componentGrades,
                        AverageScore = averageScore,
                        FinalLetterGrade = finalLetterGrade ?? string.Empty
                    });
                }

                // Sort students
                report.Students = request.SortOrder?.ToLower() == "desc"
                    ? report.Students.OrderByDescending(s => s.StudentCode).ToList()
                    : report.Students.OrderBy(s => s.StudentCode).ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class grades for class {ClassId}", classId);
                return null;
            }
        }

        public async Task<StudentGradeTranscriptDto?> GetStudentGradesAsync(Guid studentId, GetStudentGradesRequest request)
        {
            try
            {
                // Optimized: Removed heavy student details loading
                // var student = await _uow.Students.GetByIdWithDetailsAsync(studentId);

                List<Grade> grades;

                if (request.SubjectId.HasValue)
                {
                    grades = await _uow.Grades.GetGradesByStudentAndSubjectAsync(studentId, request.SubjectId.Value);
                }
                else
                {
                    grades = await _uow.Grades.GetGradesByStudentIdAsync(studentId);
                }

                // TODO: subject no longer has SemesterId - need to filter through Class/Enrollment
                if (request.SemesterId.HasValue)
                {
                    // Need to implement filtering through: Grade -> Student -> Enrollment -> Class -> SubjectOffering -> Semester
                    _logger.LogWarning("Semester filtering for grades not yet implemented with new Subject model");
                    // grades = grades.Where(g => g.Subject.SemesterId == request.SemesterId.Value).ToList();
                }

                var subjectGrades = grades
                    .GroupBy(g => g.SubjectId)
                    .Select(group =>
                    {
                        var firstGrade = group.First();
                        var componentGrades = group.Select(g => new ComponentGradeDto
                        {
                            GradeId = g.Id,
                            GradeComponentId = g.GradeComponentId,
                            ComponentName = g.GradeComponent.Name,
                            ComponentWeight = g.GradeComponent.WeightPercent,
                            Score = g.Score,
                            LetterGrade = g.LetterGrade ?? string.Empty
                        }).ToList();

                        decimal totalWeightedScore = 0;
                        int totalWeight = 0;

                        foreach (var componentGrade in componentGrades)
                        {
                            // Skip Attendance component in final score calculation
                            if (componentGrade.ComponentName.Contains("Attendance", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (componentGrade.Score.HasValue)
                            {
                                totalWeightedScore += componentGrade.Score.Value * componentGrade.ComponentWeight;
                                totalWeight += componentGrade.ComponentWeight;
                            }
                        }

                        decimal? averageScore = totalWeight > 0 ? totalWeightedScore / totalWeight : null;
                        string? finalLetterGrade = averageScore.HasValue
                            ? GradeHelper.CalculateLetterGrade(averageScore.Value)
                            : null;

                        return new SubjectGradeDto
                        {
                            SubjectId = firstGrade.SubjectId,
                            SubjectCode = firstGrade.Subject.SubjectCode,
                            SubjectName = firstGrade.Subject.SubjectName,
                            Credits = firstGrade.Subject.Credits,
                            // TODO: determine SemesterName via Grade -> Enrollment -> Class -> SubjectOffering -> Semester
                            SemesterName = "N/A",
                            ComponentGrades = componentGrades,
                            AverageScore = averageScore,
                            FinalLetterGrade = finalLetterGrade ?? string.Empty
                        };
                    })
                    .ToList();

                // Sort subjects
                subjectGrades = request.SortOrder?.ToLower() == "desc"
                    ? subjectGrades.OrderByDescending(s => s.SubjectCode).ToList()
                    : subjectGrades.OrderBy(s => s.SubjectCode).ToList();

                return new StudentGradeTranscriptDto
                {
                    StudentId = studentId,
                    StudentCode = string.Empty, // Removed redundant info
                    StudentName = string.Empty, // Removed redundant info
                    Email = string.Empty,       // Removed redundant info
                    CurrentGPA = 0,             // Removed redundant info
                    Subjects = subjectGrades
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student grades for student {StudentId}", studentId);
                return null;
            }
        }

        public async Task<Domain.DTOs.Common.PagedResult<GradeDto>> GetAllGradesAsync(GetGradesRequest request)
        {
            try
            {
                var query = _uow.Grades.GetQueryable();

                // Apply filters
                if (request.StudentId.HasValue)
                {
                    query = query.Where(g => g.StudentId == request.StudentId.Value);
                }

                if (request.SubjectId.HasValue)
                {
                    query = query.Where(g => g.SubjectId == request.SubjectId.Value);
                }

                if (request.GradeComponentId.HasValue)
                {
                    query = query.Where(g => g.GradeComponentId == request.GradeComponentId.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "studentcode" => request.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(g => g.Student.StudentCode)
                        : query.OrderBy(g => g.Student.StudentCode),
                    "studentname" => request.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(g => g.Student.User.FullName)
                        : query.OrderBy(g => g.Student.User.FullName),
                    "subjectcode" => request.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(g => g.Subject.SubjectCode)
                        : query.OrderBy(g => g.Subject.SubjectCode),
                    "score" => request.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(g => g.Score)
                        : query.OrderBy(g => g.Score),
                    _ => request.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(g => g.UpdatedAt)
                        : query.OrderBy(g => g.UpdatedAt)
                };

                // Apply pagination
                var grades = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Include(g => g.Student)
                        .ThenInclude(s => s.User)
                    .Include(g => g.Subject)
                    .Include(g => g.GradeComponent)
                    .ToListAsync();

                var gradeDtos = grades.Select(g => new GradeDto
                {
                    Id = g.Id,
                    StudentId = g.StudentId,
                    StudentCode = g.Student.StudentCode,
                    StudentName = g.Student.User.FullName,
                    SubjectId = g.SubjectId,
                    SubjectCode = g.Subject.SubjectCode,
                    SubjectName = g.Subject.SubjectName,
                    GradeComponentId = g.GradeComponentId,
                    ComponentName = g.GradeComponent.Name,
                    ComponentWeight = g.GradeComponent.WeightPercent,
                    Score = g.Score ?? 0,
                    LetterGrade = g.LetterGrade ?? string.Empty,
                    UpdatedAt = g.UpdatedAt
                }).ToList();

                return new Domain.DTOs.Common.PagedResult<GradeDto>(
                    gradeDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all grades");
                throw;
            }
        }
    }
}
