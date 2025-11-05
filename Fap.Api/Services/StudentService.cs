using AutoMapper;
using Fap.Domain.DTOs.Student;
using Fap.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class StudentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public StudentService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // ========== GET ALL STUDENTS ==========
        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _uow.Students.GetAllWithUsersAsync();

            return students.Select(s => new StudentDto
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
                TotalClasses = s.ClassMembers?.Count ?? 0
            }).ToList();
        }

        // ========== GET STUDENT BY ID WITH DETAILS ==========
        public async Task<StudentDetailDto?> GetStudentByIdAsync(Guid id)
        {
            var student = await _uow.Students.GetByIdWithDetailsAsync(id);
            if (student == null) return null;

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
                
                // Enrollments
                Enrollments = student.Enrolls?.Select(e => new EnrollmentInfo
                {
                    Id = e.Id,
                    ClassCode = e.Class?.ClassCode ?? "N/A",
                    SubjectName = e.Class?.Subject?.SubjectName ?? "N/A",
                    TeacherName = e.Class?.Teacher?.User?.FullName ?? "N/A",
                    RegisteredAt = e.RegisteredAt,
                    IsApproved = e.IsApproved
                }).ToList() ?? new List<EnrollmentInfo>(),
                
                // Current Classes
                CurrentClasses = student.ClassMembers?.Select(cm => new ClassInfo
                {
                    ClassId = cm.Class.Id,
                    ClassCode = cm.Class.ClassCode,
                    SubjectName = cm.Class.Subject?.SubjectName ?? "N/A",
                    SubjectCode = cm.Class.Subject?.SubjectCode ?? "N/A",
                    Credits = cm.Class.Subject?.Credits ?? 0,
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
    }
}
