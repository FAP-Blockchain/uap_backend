using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Specialization;
using Fap.Domain.DTOs.Teacher;
using Fap.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<TeacherService> _logger;

        public TeacherService(IUnitOfWork uow, IMapper mapper, ILogger<TeacherService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // ========== GET TEACHERS WITH PAGINATION ==========
        public async Task<PagedResult<TeacherDto>> GetTeachersAsync(GetTeachersRequest request)
        {
            try
            {
                var (teachers, totalCount) = await _uow.Teachers.GetPagedTeachersAsync(
                    request.Page,
                    request.PageSize,
                    request.SearchTerm,
                    request.SpecializationKeyword,
                    request.SpecializationId,
                    request.IsActive,
                    request.SortBy,
                    request.SortOrder
                );

                var teacherDtos = teachers.Select(t => new TeacherDto
                {
                    Id = t.Id,
                    TeacherCode = t.TeacherCode,
                    FullName = t.User?.FullName ?? "N/A",
                    Email = t.User?.Email ?? "N/A",
                    HireDate = t.HireDate,
                    Specialization = t.Specialization,
                    PhoneNumber = t.User?.PhoneNumber,
                    IsActive = t.User?.IsActive ?? false,
                    TotalClasses = t.Classes?.Count ?? 0,
                    ProfileImageUrl = t.User?.ProfileImageUrl,
                    Specializations = MapSpecializations(t)
                }).ToList();

                return new PagedResult<TeacherDto>(
                    teacherDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting teachers: {ex.Message}");
                throw;
            }
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
                Specialization = t.Specialization,
                PhoneNumber = t.User?.PhoneNumber,
                IsActive = t.User?.IsActive ?? false,
                TotalClasses = t.Classes?.Count ?? 0,
                ProfileImageUrl = t.User?.ProfileImageUrl,
                Specializations = MapSpecializations(t)
            }).ToList();
        }

        // ========== GET TEACHER BY ID WITH DETAILS ==========
        public async Task<TeacherDetailDto?> GetTeacherByIdAsync(Guid id)
        {
            try
            {
                var teacher = await _uow.Teachers.GetByIdWithDetailsAsync(id);
                if (teacher == null)
                    return null;

                return new TeacherDetailDto
                {
                    Id = teacher.Id,
                    TeacherCode = teacher.TeacherCode,
                    FullName = teacher.User?.FullName ?? "N/A",
                    Email = teacher.User?.Email ?? "N/A",
                    HireDate = teacher.HireDate,
                    Specialization = teacher.Specialization,
                    PhoneNumber = teacher.User?.PhoneNumber,
                    IsActive = teacher.User?.IsActive ?? false,
                    CreatedAt = teacher.User?.CreatedAt ?? DateTime.MinValue,
                    ProfileImageUrl = teacher.User?.ProfileImageUrl,
                    
                    // Classes
                    Classes = teacher.Classes?.Select(c => new TeachingClassInfo
                    {
                        ClassId = c.Id,
                        ClassCode = c.ClassCode,
                        SubjectName = c.SubjectOffering?.Subject?.SubjectName ?? "N/A",
                        SubjectCode = c.SubjectOffering?.Subject?.SubjectCode ?? "N/A",
                        Credits = c.SubjectOffering?.Subject?.Credits ?? 0,
                        SemesterName = c.SubjectOffering?.Semester?.Name ?? "N/A",
                        TotalStudents = c.Members?.Count ?? 0,
                        TotalSlots = c.Slots?.Count ?? 0
                    }).ToList() ?? new List<TeachingClassInfo>(),
        
                    // Statistics
                    TotalClasses = teacher.Classes?.Count ?? 0,
                    TotalStudents = teacher.Classes?.Sum(c => c.Members?.Count ?? 0) ?? 0,
                    Specializations = MapSpecializations(teacher)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting teacher {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<TeacherDetailDto?> GetTeacherByUserIdAsync(Guid userId)
        {
            var teacher = await _uow.Teachers.GetByUserIdAsync(userId);
            if (teacher == null)
            {
                return null;
            }

            return await GetTeacherByIdAsync(teacher.Id);
        }

        private static List<SpecializationDto> MapSpecializations(Domain.Entities.Teacher teacher)
        {
            return teacher.TeacherSpecializations?
                       .Select(ts => new SpecializationDto
                       {
                           Id = ts.SpecializationId,
                           Code = ts.Specialization?.Code ?? string.Empty,
                           Name = ts.Specialization?.Name ?? string.Empty,
                           Description = ts.Specialization?.Description,
                           IsActive = ts.Specialization?.IsActive ?? true
                       })
                       .OrderBy(s => s.Name)
                       .ToList() ?? new List<SpecializationDto>();
        }
    }
}
