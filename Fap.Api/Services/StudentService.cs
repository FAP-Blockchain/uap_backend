using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Student;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<StudentService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // ========== GET STUDENTS WITH PAGINATION ==========
        public async Task<PagedResult<StudentDto>> GetStudentsAsync(GetStudentsRequest request)
        {
            try
            {
                var (students, totalCount) = await _uow.Students.GetPagedStudentsAsync(
                    request.Page,
                    request.PageSize,
                    request.SearchTerm,
                    request.IsGraduated,
                    request.IsActive,
                    request.MinGPA,
                    request.MaxGPA,
                    request.SortBy,
                    request.SortOrder
                );

                var studentDtos = students.Select(s => new StudentDto
                {
                    Id = s.Id,
                    StudentCode = s.StudentCode,
                    FullName = s.User?.FullName ?? "N/A",
                    Email = s.User?.Email ?? "N/A",
                    EnrollmentDate = s.EnrollmentDate,
                    GPA = s.GPA,
                    IsGraduated = s.IsGraduated,
                    GraduationDate = s.GraduationDate,
                    IsActive = s.User?.IsActive ?? false,
                    TotalEnrollments = s.Enrolls?.Count ?? 0,
                    TotalClasses = s.ClassMembers?.Count ?? 0,
                    ProfileImageUrl = s.User?.ProfileImageUrl
                }).ToList();

                return new PagedResult<StudentDto>(
                    studentDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting students: {ex.Message}");
                throw;
            }
        }

        // ========== GET STUDENT BY ID WITH DETAILS ==========
        public async Task<StudentDetailDto?> GetStudentByIdAsync(Guid id)
        {
            try
            {
                var student = await _uow.Students.GetByIdWithDetailsAsync(id);
                if (student == null)
                    return null;

                return new StudentDetailDto
                {
                    Id = student.Id,
                    StudentCode = student.StudentCode,
                    FullName = student.User?.FullName ?? "N/A",
                    Email = student.User?.Email ?? "N/A",
                    EnrollmentDate = student.EnrollmentDate,
                    GPA = student.GPA,
                    IsGraduated = student.IsGraduated,
                    GraduationDate = student.GraduationDate,
                    IsActive = student.User?.IsActive ?? false,
                    CreatedAt = student.User?.CreatedAt ?? DateTime.MinValue,
                    
                    // Contact & blockchain info
                    PhoneNumber = student.User?.PhoneNumber,
                    WalletAddress = student.User?.WalletAddress,
                    ProfileImageUrl = student.User?.ProfileImageUrl,
                    
                    // Enrollments
                    Enrollments = student.Enrolls?.Select(e => new EnrollmentInfo
                    {
                        Id = e.Id,
                        ClassCode = e.Class?.ClassCode ?? "N/A",
                        SubjectName = e.Class?.SubjectOffering?.Subject?.SubjectName ?? "N/A",
                        TeacherName = e.Class?.Teacher?.User?.FullName ?? "N/A",
                        RegisteredAt = e.RegisteredAt,
                        IsApproved = e.IsApproved
                    }).ToList() ?? new List<EnrollmentInfo>(),
                    
                    // Current Classes
                    CurrentClasses = student.ClassMembers?.Select(cm => new ClassInfo
                    {
                        ClassId = cm.Class.Id,
                        ClassCode = cm.Class.ClassCode,
                        SubjectName = cm.Class.SubjectOffering?.Subject?.SubjectName ?? "N/A",
                        SubjectCode = cm.Class.SubjectOffering?.Subject?.SubjectCode ?? "N/A",
                        Credits = cm.Class.SubjectOffering?.Subject?.Credits ?? 0,
                        TeacherName = cm.Class.Teacher?.User?.FullName ?? "N/A",
                        JoinedAt = cm.JoinedAt
                    }).ToList() ?? new List<ClassInfo>(),
                    
                    // Statistics
                    TotalEnrollments = student.Enrolls?.Count ?? 0,
                    ApprovedEnrollments = student.Enrolls?.Count(e => e.IsApproved) ?? 0,
                    PendingEnrollments = student.Enrolls?.Count(e => !e.IsApproved) ?? 0,
                    TotalClasses = student.ClassMembers?.Count ?? 0,
                    TotalGrades = student.Grades?.Count ?? 0,
                    TotalAttendances = student.Attendances?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<StudentDetailDto?> GetStudentByUserIdAsync(Guid userId)
        {
            try
            {
                var student = await _uow.Students.GetByUserIdAsync(userId);
                if (student == null)
                {
                    return null;
                }

                return await GetStudentByIdAsync(student.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting student profile for user {userId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get current logged-in student's profile with basic details (no enrollments/classes)
        /// </summary>
    public async Task<StudentSelfProfileDto?> GetCurrentStudentProfileAsync(Guid userId)
        {
            try
            {
                var student = await _uow.Students.GetByUserIdAsync(userId);
                if (student == null)
                {
                    _logger.LogWarning($"No student found for user {userId}");
                    return null;
                }

                var totalClasses = await _uow.ClassMembers.GetQueryable()
                    .AsNoTracking()
                    .CountAsync(cm => cm.StudentId == student.Id);

                var profile = new StudentSelfProfileDto
                {
                    Id = student.Id,
                    StudentCode = student.StudentCode,
                    FullName = student.User?.FullName ?? "N/A",
                    Email = student.User?.Email ?? "N/A",
                    EnrollmentDate = student.EnrollmentDate,
                    GPA = student.GPA,
                    IsGraduated = student.IsGraduated,
                    IsActive = student.User?.IsActive ?? false,
                    CreatedAt = student.User?.CreatedAt ?? DateTime.MinValue,
                    WalletAddress = student.User?.WalletAddress,
                    ProfileImageUrl = student.User?.ProfileImageUrl,
                    TotalClasses = totalClasses
                };

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current student profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get students eligible for a specific class
        /// 
    /// Curriculum-based eligibility logic:
        /// 1. Student must have a curriculum that includes this subject
        /// 2. Student must not be graduated
        /// 3. Student must be active
        /// 4. Student must not have completed this subject (no passing grade >= 5.0)
        /// 5. Student must not be enrolled in this specific class
        /// 6. Student must not be enrolled in any other class for this subject in this semester
        /// 7. Student must have passed all prerequisite subjects (from CurriculumSubject table)
        /// 
        /// Example: To enroll in SE201 (Software Engineering), student must:
        /// - Have SE201 in their curriculum (e.g., SE-2024)
        /// - Not have passed SE201 yet
        /// - Have passed CS101 (prerequisite defined in CurriculumSubject)
        /// - Not already be enrolled in any SE201 class this semester
        /// </summary>
        public async Task<PagedResult<StudentDto>> GetEligibleStudentsForClassAsync(
            Guid classId,
            int page = 1,
            int pageSize = 20,
            string? searchTerm = null)
        {
            try
            {
                // Get class details
                var classEntity = await _uow.Classes.GetByIdWithDetailsAsync(classId);
                if (classEntity == null)
                {
                    _logger.LogWarning("Class {ClassId} not found", classId);
                    return new PagedResult<StudentDto>(
                        new List<StudentDto>(),
                        0,
                        page,
                        pageSize
                    );
                }

                var subjectId = classEntity.SubjectOffering.SubjectId;
                var semesterId = classEntity.SubjectOffering.SemesterId;

                _logger.LogInformation(
                    "Getting eligible students for class {ClassCode} (Subject: {SubjectId}, Semester: {SemesterId})",
                    classEntity.ClassCode, subjectId, semesterId);

                // Get eligible students using curriculum-based logic
                var (students, totalCount) = await _uow.Students.GetEligibleStudentsForSubjectAsync(
                    subjectId,
                    semesterId,
                    classId,
                    page,
                    pageSize,
                    searchTerm
                );

                var studentDtos = students.Select(s => new StudentDto
                {
                    Id = s.Id,
                    StudentCode = s.StudentCode,
                    FullName = s.User?.FullName ?? "N/A",
                    Email = s.User?.Email ?? "N/A",
                    EnrollmentDate = s.EnrollmentDate,
                    GPA = s.GPA,
                    IsGraduated = s.IsGraduated,
                    GraduationDate = s.GraduationDate,
                    IsActive = s.User?.IsActive ?? false,
                    TotalEnrollments = s.Enrolls?.Count ?? 0,
                    TotalClasses = s.ClassMembers?.Count ?? 0,
                    ProfileImageUrl = s.User?.ProfileImageUrl
                }).ToList();

                _logger.LogInformation(
                    "Found {Count} eligible students for class {ClassCode}",
                    totalCount, classEntity.ClassCode);

                return new PagedResult<StudentDto>(
                    studentDtos,
                    totalCount,
                    page,
                    pageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting eligible students for class {ClassId}", classId);
                throw;
            }
        }
    }
}
