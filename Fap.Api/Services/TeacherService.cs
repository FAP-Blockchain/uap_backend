using AutoMapper;
using Fap.Domain.DTOs.Teacher;
using Fap.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class TeacherService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public TeacherService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // ========== GET ALL TEACHERS ==========
        public async Task<List<TeacherDto>> GetAllTeachersAsync()
        {
            var teachers = await _uow.Teachers.GetAllWithUsersAsync();

            return teachers.Select(t => new TeacherDto
            {
                Id = t.Id,
                TeacherCode = t.TeacherCode,
                FullName = t.User?.FullName ?? "N/A",
                Email = t.User?.Email ?? "N/A",
                HireDate = t.HireDate,
                Specialization = t.Specialization ?? "N/A",
                PhoneNumber = t.PhoneNumber ?? "N/A",
                IsActive = t.User?.IsActive ?? false,
                TotalClasses = t.Classes?.Count ?? 0
            }).ToList();
        }

        // ========== GET TEACHER BY ID WITH DETAILS ==========
        public async Task<TeacherDetailDto?> GetTeacherByIdAsync(Guid id)
        {
            var teacher = await _uow.Teachers.GetByIdWithDetailsAsync(id);
            if (teacher == null) return null;

            return new TeacherDetailDto
            {
                Id = teacher.Id,
                TeacherCode = teacher.TeacherCode,
                FullName = teacher.User?.FullName ?? "N/A",
                Email = teacher.User?.Email ?? "N/A",
                HireDate = teacher.HireDate,
                Specialization = teacher.Specialization ?? "N/A",
                PhoneNumber = teacher.PhoneNumber ?? "N/A",
                IsActive = teacher.User?.IsActive ?? false,
                CreatedAt = teacher.User?.CreatedAt ?? DateTime.MinValue,
                
                // Classes
                Classes = teacher.Classes?.Select(c => new TeachingClassInfo
                {
                    ClassId = c.Id,
                    ClassCode = c.ClassCode,
                    SubjectName = c.Subject?.SubjectName ?? "N/A",
                    SubjectCode = c.Subject?.SubjectCode ?? "N/A",
                    Credits = c.Subject?.Credits ?? 0,
                    SemesterName = c.Subject?.Semester?.Name ?? "N/A",
                    TotalStudents = c.Members?.Count ?? 0,
                    TotalSlots = c.Slots?.Count ?? 0
                }).ToList() ?? new List<TeachingClassInfo>(),
                
                // Statistics
                TotalClasses = teacher.Classes?.Count ?? 0,
                TotalStudents = teacher.Classes?.Sum(c => c.Members?.Count ?? 0) ?? 0
            };
        }
    }
}
