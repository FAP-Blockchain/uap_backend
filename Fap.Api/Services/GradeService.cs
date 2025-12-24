using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.Constants;
using Fap.Domain.DTOs;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.Entities;
using Fap.Domain.Helpers;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IBlockchainService _blockchainService;
        private readonly FapDbContext _db;
        private readonly BlockchainSettings _blockchainSettings;

        public GradeService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<GradeService> logger,
            IBlockchainService blockchainService,
            FapDbContext db,
            IOptions<BlockchainSettings> blockchainSettings)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _blockchainService = blockchainService;
            _db = db;
            _blockchainSettings = blockchainSettings.Value;
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

        // ===== ON-CHAIN (GradeManagement) =====
        public async Task<GradeOnChainPrepareDto?> PrepareGradeOnChainAsync(Guid gradeId)
        {
            try
            {
                var grade = await _uow.Grades.GetByIdWithDetailsAsync(gradeId);
                if (grade == null)
                {
                    return null;
                }

                // Tìm class on-chain gần nhất có liên quan đến subject này
                var classesForSubject = await _uow.Classes
                    .GetQueryable()
                    .Where(c => c.SubjectOffering.SubjectId == grade.SubjectId
                                && c.OnChainClassId.HasValue)
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (classesForSubject == null || !classesForSubject.OnChainClassId.HasValue)
                {
                    // Chưa on-chain class => FE phải xử lý trước
                    _logger.LogWarning(
                        "PrepareGradeOnChain: class for student {StudentId} subject {SubjectId} is null or not on-chain",
                        grade.StudentId,
                        grade.SubjectId);
                    return null;
                }

                var studentWallet = grade.Student.User.WalletAddress;
                if (string.IsNullOrWhiteSpace(studentWallet))
                {
                    _logger.LogWarning(
                        "PrepareGradeOnChain: student {StudentId} has no wallet address",
                        grade.StudentId);
                    return null;
                }

                // Chuyển score sang dạng uint cho contract (ở đây tạm nhân 100 để giữ 2 chữ số sau dấu phẩy)
                var score = grade.Score ?? 0m;
                const decimal factor = 100m;
                var onChainScore = (ulong)(score * factor);

                // Giả định MaxScore = 10 cho mọi component (giống Range(0,10))
                var maxScore = 10m;
                var onChainMaxScore = (ulong)(maxScore * factor);

                return new GradeOnChainPrepareDto
                {
                    GradeId = grade.Id,
                    StudentId = grade.StudentId,
                    StudentWalletAddress = studentWallet,
                    ClassId = classesForSubject.Id,
                    ComponentName = grade.GradeComponent.Name,
                    Score = score,
                    MaxScore = maxScore,
                    OnChainClassId = (ulong)classesForSubject.OnChainClassId.Value,
                    OnChainScore = onChainScore,
                    OnChainMaxScore = onChainMaxScore
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing grade {GradeId} for on-chain", gradeId);
                return null;
            }
        }

        public async Task<ServiceResult<bool>> SaveGradeOnChainAsync(Guid gradeId, SaveGradeOnChainRequest request, Guid performedByUserId)
        {
            var result = new ServiceResult<bool>();

            try
            {
                var grade = await _uow.Grades.GetByIdWithDetailsAsync(gradeId);
                if (grade == null)
                {
                    result.Success = false;
                    result.Message = $"Grade with ID '{gradeId}' not found";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.TransactionHash))
                {
                    result.Success = false;
                    result.Message = "TransactionHash is required";
                    return result;
                }

                var txHash = request.TransactionHash.Trim();
                if (!txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    txHash = "0x" + txHash;
                }

                if (txHash.Length != 66)
                {
                    result.Success = false;
                    result.Message = "TxHash must be 66 characters (0x + 64 hex).";
                    return result;
                }

                // 1) Fetch receipt from chain (do not trust FE for block/time)
                Nethereum.RPC.Eth.DTOs.TransactionReceipt receipt;
                try
                {
                    receipt = await _blockchainService.WaitForTransactionReceiptAsync(txHash, timeoutSeconds: 120);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch transaction receipt for TxHash={TxHash}", txHash);
                    result.Success = false;
                    result.Message = "Could not fetch transaction receipt (timeout or invalid txHash)";
                    return result;
                }

                var blockNumber = receipt.BlockNumber?.Value;
                if (blockNumber == null)
                {
                    result.Success = false;
                    result.Message = "Receipt did not include BlockNumber";
                    return result;
                }

                // 2) Decode known events for audit/validation
                IReadOnlyList<(string EventName, string ContractAddress, string DetailJson)> decodedEvents;
                try
                {
                    decodedEvents = await _blockchainService.DecodeReceiptEventsAsync(txHash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not decode receipt events for TxHash={TxHash}", txHash);
                    decodedEvents = Array.Empty<(string, string, string)>();
                }

                // Only log/validate grade-related events
                var gradeEvents = decodedEvents
                    .Where(e => string.Equals(e.EventName, "GradeRecorded", StringComparison.Ordinal))
                    .ToList();

                if (gradeEvents.Count == 0)
                {
                    result.Success = false;
                    result.Message = "Transaction receipt did not contain an expected grade event (GradeRecorded)";
                    return result;
                }

                // 3) Fetch tx for TxFrom/TxTo (auditability) + optional contract target validation
                string? txFrom = null;
                string? txTo = null;
                try
                {
                    var web3 = _blockchainService.GetWeb3();
                    var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                    txFrom = tx?.From;
                    txTo = tx?.To;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve tx from/to for TxHash={TxHash}", txHash);
                }

                var expectedContractAddress = _blockchainSettings.Contracts?.GradeManagement;
                if (!string.IsNullOrWhiteSpace(expectedContractAddress) &&
                    !string.IsNullOrWhiteSpace(txTo) &&
                    !string.Equals(txTo, expectedContractAddress, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Message = "Transaction did not target GradeManagement contract";
                    return result;
                }

                // 3.5) Resolve expected OnChainClassId (server-side) + student wallet (for validation)
                long? onChainClassId = null;
                try
                {
                    var classForSubject = await _uow.Classes
                        .GetQueryable()
                        .Where(c => c.SubjectOffering.SubjectId == grade.SubjectId && c.OnChainClassId.HasValue)
                        .OrderByDescending(c => c.UpdatedAt)
                        .FirstOrDefaultAsync();

                    onChainClassId = classForSubject?.OnChainClassId;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve OnChainClassId for GradeId={GradeId}", gradeId);
                }

                var studentWallet = grade.Student?.User?.WalletAddress;

                // Pick the most relevant GradeRecorded event (match student wallet if possible)
                (string EventName, string ContractAddress, string DetailJson) selectedEvent = gradeEvents[0];
                if (!string.IsNullOrWhiteSpace(studentWallet))
                {
                    foreach (var ev in gradeEvents)
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(ev.DetailJson);
                            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                doc.RootElement.TryGetProperty("indexedArgs", out var ia) &&
                                ia.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                ia.TryGetProperty("studentAddress", out var sa))
                            {
                                var addr = sa.GetString();
                                if (!string.IsNullOrWhiteSpace(addr) &&
                                    string.Equals(addr, studentWallet, StringComparison.OrdinalIgnoreCase))
                                {
                                    selectedEvent = ev;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // ignore parse errors; keep default
                        }
                    }
                }

                // Extract indexed args for validation + saving OnChainGradeId if FE didn't send it
                ulong? decodedOnChainGradeId = null;
                ulong? decodedOnChainClassId = null;
                string? decodedStudentAddress = null;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(selectedEvent.DetailJson);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("indexedArgs", out var ia) &&
                        ia.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (ia.TryGetProperty("gradeId", out var gid))
                        {
                            var s = gid.GetString();
                            if (!string.IsNullOrWhiteSpace(s) && ulong.TryParse(s, out var parsed))
                            {
                                decodedOnChainGradeId = parsed;
                            }
                        }
                        if (ia.TryGetProperty("classId", out var cid))
                        {
                            var s = cid.GetString();
                            if (!string.IsNullOrWhiteSpace(s) && ulong.TryParse(s, out var parsed))
                            {
                                decodedOnChainClassId = parsed;
                            }
                        }
                        if (ia.TryGetProperty("studentAddress", out var sa))
                        {
                            decodedStudentAddress = sa.GetString();
                        }
                    }
                }
                catch
                {
                    // ignore; validation below becomes best-effort
                }

                if (request.OnChainGradeId.HasValue && decodedOnChainGradeId.HasValue && request.OnChainGradeId.Value != decodedOnChainGradeId.Value)
                {
                    result.Success = false;
                    result.Message = "OnChainGradeId does not match decoded GradeRecorded event";
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(studentWallet) && !string.IsNullOrWhiteSpace(decodedStudentAddress) &&
                    !string.Equals(decodedStudentAddress, studentWallet, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.Message = "Transaction GradeRecorded studentAddress does not match student's wallet";
                    return result;
                }

                if (onChainClassId.HasValue && decodedOnChainClassId.HasValue && decodedOnChainClassId.Value != (ulong)onChainClassId.Value)
                {
                    result.Success = false;
                    result.Message = "Transaction GradeRecorded classId does not match server-resolved OnChainClassId";
                    return result;
                }

                grade.OnChainTxHash = txHash;
                grade.OnChainBlockNumber = (long)blockNumber;
                grade.OnChainChainId = request.ChainId > 0 ? request.ChainId : _blockchainSettings.ChainId;
                grade.OnChainContractAddress = !string.IsNullOrWhiteSpace(txTo)
                    ? txTo
                    : (!string.IsNullOrWhiteSpace(expectedContractAddress) ? expectedContractAddress : request.ContractAddress);

                var onChainGradeId = request.OnChainGradeId ?? decodedOnChainGradeId;
                if (onChainGradeId.HasValue)
                {
                    grade.OnChainGradeId = onChainGradeId.Value;
                }

                _uow.Grades.Update(grade);

                // 4) Persist ActionLogs for audit trail (actor is whoever performed the sync)
                string detail;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(selectedEvent.DetailJson);
                    var root = doc.RootElement;
                    object? indexedArgs = null;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("indexedArgs", out var ia))
                    {
                        indexedArgs = ia.Clone();
                    }

                    detail = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        gradeId,
                        studentId = grade.StudentId,
                        subjectId = grade.SubjectId,
                        gradeComponentId = grade.GradeComponentId,
                        score = grade.Score,
                        onChainClassId,
                        onChainGradeId,
                        contractAddress = selectedEvent.ContractAddress,
                        eventName = selectedEvent.EventName,
                        indexedArgs
                    });
                }
                catch
                {
                    detail = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        gradeId,
                        studentId = grade.StudentId,
                        subjectId = grade.SubjectId,
                        gradeComponentId = grade.GradeComponentId,
                        score = grade.Score,
                        onChainClassId,
                        onChainGradeId,
                        contractAddress = selectedEvent.ContractAddress,
                        eventName = selectedEvent.EventName,
                        decoded = selectedEvent.DetailJson
                    });
                }

                if (detail.Length > 500)
                {
                    detail = detail.Substring(0, 500);
                }

                _db.ActionLogs.Add(new ActionLog
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                        Action = ActionLogActions.GradeOnChainSync,
                    Detail = detail,
                    UserId = performedByUserId,
                    TransactionHash = txHash,
                    BlockNumber = (long)blockNumber,
                    EventName = selectedEvent.EventName,
                    TxFrom = txFrom,
                    TxTo = txTo,
                    ContractAddress = selectedEvent.ContractAddress,
                    CredentialId = null
                });

                await _uow.SaveChangesAsync();

                result.Success = true;
                result.Data = true;
                result.Message = "Grade on-chain info saved successfully";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving on-chain info for grade {GradeId}", gradeId);
                result.Success = false;
                result.Message = "Failed to save on-chain info for grade";
                return result;
            }
        }

        public async Task<List<GradeVerifyItemDto>> VerifyGradeListAsync(Guid studentId, Guid classId)
        {
            var classEntity = await _db.Classes
                .AsNoTracking()
                .Include(c => c.SubjectOffering)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
            {
                throw new InvalidOperationException("Class not found");
            }

            if (!classEntity.OnChainClassId.HasValue)
            {
                throw new InvalidOperationException("Class is not on-chain (missing OnChainClassId)");
            }

            var student = await _db.Students
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                throw new InvalidOperationException("Student not found");
            }

            var wallet = student.User?.WalletAddress;
            if (string.IsNullOrWhiteSpace(wallet))
            {
                throw new InvalidOperationException("Student wallet address not found");
            }

            var subjectId = classEntity.SubjectOffering.SubjectId;

            var grades = await _db.Grades
                .AsNoTracking()
                .Where(g => g.StudentId == studentId && g.SubjectId == subjectId)
                .Include(g => g.Student).ThenInclude(s => s.User)
                .Include(g => g.Subject)
                .Include(g => g.GradeComponent)
                .OrderBy(g => g.UpdatedAt)
                .ToListAsync();

            var results = new List<GradeVerifyItemDto>();

            foreach (var grade in grades)
            {
                var item = new GradeVerifyItemDto
                {
                    Grade = _mapper.Map<GradeDto>(grade),
                    Verified = false,
                    Message = string.Empty
                };

                if (!grade.OnChainGradeId.HasValue || grade.OnChainGradeId.Value == 0)
                {
                    item.Message = "Missing on-chain gradeId";
                    results.Add(item);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(grade.OnChainTxHash))
                {
                    item.Message = "Missing on-chain txHash";
                    results.Add(item);
                    continue;
                }

                try
                {
                    // 1) Resolve gradeId from tx receipt (anti-tamper)
                    var decodedEvents = await _blockchainService.DecodeReceiptEventsAsync(grade.OnChainTxHash);
                    var gradeEvents = decodedEvents
                        .Where(e => string.Equals(e.EventName, "GradeRecorded", StringComparison.Ordinal))
                        .ToList();

                    if (gradeEvents.Count == 0)
                    {
                        item.Message = "Tx receipt did not contain GradeRecorded";
                        results.Add(item);
                        continue;
                    }

                    ulong? receiptGradeId = null;
                    ulong? receiptClassId = null;
                    string? receiptStudentAddress = null;

                    foreach (var ev in gradeEvents)
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(ev.DetailJson);
                            var root = doc.RootElement;
                            if (root.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                            if (!root.TryGetProperty("indexedArgs", out var ia) || ia.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                            var gidStr = ia.TryGetProperty("gradeId", out var gidEl) ? gidEl.GetString() : null;
                            var cidStr = ia.TryGetProperty("classId", out var cidEl) ? cidEl.GetString() : null;
                            var saStr = ia.TryGetProperty("studentAddress", out var saEl) ? saEl.GetString() : null;

                            if (!string.IsNullOrWhiteSpace(cidStr) && ulong.TryParse(cidStr, out var cidUlong) && cidUlong == (ulong)classEntity.OnChainClassId.Value &&
                                !string.IsNullOrWhiteSpace(saStr) && string.Equals(saStr, wallet, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrWhiteSpace(gidStr) && ulong.TryParse(gidStr, out var gidUlong) && gidUlong > 0)
                            {
                                receiptGradeId = gidUlong;
                                receiptClassId = cidUlong;
                                receiptStudentAddress = saStr;
                                break;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    if (!receiptGradeId.HasValue)
                    {
                        // fallback: first gradeId found
                        foreach (var ev in gradeEvents)
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(ev.DetailJson);
                                var root = doc.RootElement;
                                if (root.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                                if (!root.TryGetProperty("indexedArgs", out var ia) || ia.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                                if (ia.TryGetProperty("gradeId", out var gidEl))
                                {
                                    var gidStr = gidEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(gidStr) && ulong.TryParse(gidStr, out var gidUlong) && gidUlong > 0)
                                    {
                                        receiptGradeId = gidUlong;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    if (!receiptGradeId.HasValue || receiptGradeId.Value == 0)
                    {
                        item.Message = "Could not resolve gradeId from tx receipt";
                        results.Add(item);
                        continue;
                    }

                    if ((ulong)grade.OnChainGradeId.Value != receiptGradeId.Value)
                    {
                        item.Message = "Mismatch: gradeId (DB vs tx receipt)";
                        results.Add(item);
                        continue;
                    }

                    // 2) Query chain state and compare
                    var chainGrade = await _blockchainService.GetGradeFromChainAsync((long)receiptGradeId.Value);

                    if ((long)chainGrade.ClassId != classEntity.OnChainClassId.Value)
                    {
                        item.Message = "Mismatch: classId";
                        results.Add(item);
                        continue;
                    }

                    if (!string.Equals(chainGrade.StudentAddress, wallet, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = "Mismatch: student wallet";
                        results.Add(item);
                        continue;
                    }

                    // Optional: if we decoded student/class from receipt, ensure receipt matches too
                    if (receiptClassId.HasValue && (long)receiptClassId.Value != classEntity.OnChainClassId.Value)
                    {
                        item.Message = "Mismatch: classId (tx receipt)";
                        results.Add(item);
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(receiptStudentAddress) && !string.Equals(receiptStudentAddress, wallet, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Message = "Mismatch: student wallet (tx receipt)";
                        results.Add(item);
                        continue;
                    }

                    var dbComponent = (grade.GradeComponent?.Name ?? string.Empty).Trim();
                    var chainComponent = (chainGrade.ComponentName ?? string.Empty).Trim();
                    if (!string.Equals(dbComponent, chainComponent, StringComparison.Ordinal))
                    {
                        item.Message = "Mismatch: componentName";
                        results.Add(item);
                        continue;
                    }

                    const decimal factor = 100m;
                    var dbScore = grade.Score ?? 0m;
                    var expectedOnChainScore = (ulong)(dbScore * factor);
                    var expectedOnChainMaxScore = 1000UL; // 10.00 * 100

                    if (chainGrade.Score != new System.Numerics.BigInteger(expectedOnChainScore))
                    {
                        item.Message = "Mismatch: score";
                        results.Add(item);
                        continue;
                    }

                    if (chainGrade.MaxScore != new System.Numerics.BigInteger(expectedOnChainMaxScore))
                    {
                        item.Message = "Mismatch: maxScore";
                        results.Add(item);
                        continue;
                    }

                    item.Verified = true;
                    item.Message = "Verified";
                    results.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to verify grade {GradeId} using on-chain grade {OnChainGradeId}", grade.Id, grade.OnChainGradeId);
                    item.Message = "Failed to query on-chain grade";
                    results.Add(item);
                }
            }

            return results;
        }
    }
}
