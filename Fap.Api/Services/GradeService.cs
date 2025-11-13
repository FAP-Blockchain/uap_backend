using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.Entities;
using Fap.Domain.Helpers;
using Fap.Domain.Repositories;
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

                // ? Auto-calculate letter grade based on score
                var letterGrade = GradeHelper.CalculateLetterGrade(request.Score);

                var newGrade = new Grade
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    SubjectId = request.SubjectId,
                    GradeComponentId = request.GradeComponentId,
                    Score = request.Score,
                    LetterGrade = letterGrade,  // ? Always auto-calculated
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
                    Score = grade.Score,
                    LetterGrade = grade.LetterGrade,
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

                // ? Auto-calculate letter grade based on new score
                var letterGrade = GradeHelper.CalculateLetterGrade(request.Score);

                grade.Score = request.Score;
                grade.LetterGrade = letterGrade;  // ? Always auto-calculated
                grade.UpdatedAt = DateTime.UtcNow;

                _uow.Grades.Update(grade);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade updated successfully";
                response.GradeId = grade.Id;

                _logger.LogInformation("Grade {GradeId} updated", id);

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
                    SubjectCode = classEntity.Subject.SubjectCode,
                    SubjectName = classEntity.Subject.SubjectName,
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
                            LetterGrade = grade?.LetterGrade
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
                    string finalLetterGrade = null;

                    if (gradesWithScores.Any())
                    {
                        decimal totalWeightedScore = 0;
                        int totalWeight = 0;

                        foreach (var cg in gradesWithScores)
                        {
                            totalWeightedScore += cg.Score.Value * cg.ComponentWeight;
                            totalWeight += cg.ComponentWeight;
                        }

                        if (totalWeight > 0)
                        {
                            averageScore = totalWeightedScore / totalWeight;
                            finalLetterGrade = GradeHelper.CalculateLetterGrade(averageScore.Value);
                        }
                    }

                    report.Students.Add(new StudentGradeInClassDto
                    {
                        StudentId = member.StudentId,
                        StudentCode = member.Student.StudentCode,
                        StudentName = member.Student.User.FullName,
                        Grades = componentGrades,
                        AverageScore = averageScore,
                        FinalLetterGrade = finalLetterGrade
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
                var student = await _uow.Students.GetByIdWithDetailsAsync(studentId);
                if (student == null)
                    return null;

                // Get student's grades
                var grades = await _uow.Grades.GetGradesByStudentIdAsync(studentId);

                // Filter by semester if requested
                if (request.SemesterId.HasValue)
                {
                    grades = grades.Where(g => g.Subject.SemesterId == request.SemesterId.Value).ToList();
                }

                // Filter by subject if requested
                if (request.SubjectId.HasValue)
                {
                    grades = grades.Where(g => g.SubjectId == request.SubjectId.Value).ToList();
                }

                // Group grades by subject
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
                            LetterGrade = g.LetterGrade
                        }).ToList();

                        // Calculate average for subject
                        decimal totalWeightedScore = 0;
                        int totalWeight = 0;

                        foreach (var cg in componentGrades)
                        {
                            if (cg.Score.HasValue)
                            {
                                totalWeightedScore += cg.Score.Value * cg.ComponentWeight;
                                totalWeight += cg.ComponentWeight;
                            }
                        }

                        decimal? averageScore = totalWeight > 0 ? totalWeightedScore / totalWeight : null;
                        string finalLetterGrade = averageScore.HasValue ? GradeHelper.CalculateLetterGrade(averageScore.Value) : null;

                        return new SubjectGradeDto
                        {
                            SubjectId = firstGrade.SubjectId,
                            SubjectCode = firstGrade.Subject.SubjectCode,
                            SubjectName = firstGrade.Subject.SubjectName,
                            Credits = firstGrade.Subject.Credits,
                            SemesterName = firstGrade.Subject.Semester.Name,
                            ComponentGrades = componentGrades,
                            AverageScore = averageScore,
                            FinalLetterGrade = finalLetterGrade
                        };
                    })
                    .ToList();

                // Sort subjects
                subjectGrades = request.SortOrder?.ToLower() == "desc"
                    ? subjectGrades.OrderByDescending(s => s.SubjectCode).ToList()
                    : subjectGrades.OrderBy(s => s.SubjectCode).ToList();

                return new StudentGradeTranscriptDto
                {
                    StudentId = student.Id,
                    StudentCode = student.StudentCode,
                    StudentName = student.User.FullName,
                    Email = student.User.Email,
                    CurrentGPA = student.GPA,
                    Subjects = subjectGrades
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student grades for student {StudentId}", studentId);
                return null;
            }
        }
    }
}
