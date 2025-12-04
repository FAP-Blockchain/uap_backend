using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Specialization;
using Fap.Domain.DTOs.Subject;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fap.Api.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<SubjectService> _logger;

        public SubjectService(IUnitOfWork uow, IMapper mapper, ILogger<SubjectService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(IEnumerable<SubjectDto> Subjects, int TotalCount)> GetSubjectsAsync(GetSubjectsRequest request)
        {
            var query = await _uow.Subjects.GetAllWithDetailsAsync();

                        // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(s =>
                                        s.SubjectCode.ToLower().Contains(searchLower) ||
                                        s.SubjectName.ToLower().Contains(searchLower)
                                );
            }

            if (request.SemesterId.HasValue)
            {
                query = query.Where(s => s.Offerings.Any(o => o.SemesterId == request.SemesterId.Value));
            }

            // Sorting
            query = request.SortBy.ToLower() switch
            {
                "subjectname" => request.IsDescending
                                        ? query.OrderByDescending(s => s.SubjectName)
                                        : query.OrderBy(s => s.SubjectName),
                "credits" => request.IsDescending
                                        ? query.OrderByDescending(s => s.Credits)
                                        : query.OrderBy(s => s.Credits),
                _ => request.IsDescending
                                        ? query.OrderByDescending(s => s.SubjectCode)
                                        : query.OrderBy(s => s.SubjectCode)
            };

            var totalCount = query.Count();

            // Pagination
            var subjects = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Credits = s.Credits,
                    Description = s.Description,
                    Category = s.Category,
                    Department = s.Department,
                    Prerequisites = s.Prerequisites,
                    TotalOfferings = s.Offerings.Count,
                    Specializations = MapSpecializations(s)
                })
                .ToList();

            return (subjects, totalCount);
        }

        public async Task<SubjectDetailDto?> GetSubjectByIdAsync(Guid id)
        {
            var subject = await _uow.Subjects.GetByIdWithDetailsAsync(id);
            if (subject == null) return null;

            return new SubjectDetailDto
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Credits = subject.Credits,
                Description = subject.Description,
                Category = subject.Category,
                Department = subject.Department,
                Prerequisites = subject.Prerequisites,
                Offerings = subject.Offerings.Select(o => new SubjectOfferingDto
                {
                    Id = o.Id,
                    SubjectId = o.SubjectId,
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    Credits = subject.Credits,
                    SemesterId = o.SemesterId,
                    SemesterName = o.Semester?.Name ?? "N/A",
                    MaxClasses = o.MaxClasses,
                    SemesterCapacity = o.SemesterCapacity,
                    RegistrationStartDate = o.RegistrationStartDate,
                    RegistrationEndDate = o.RegistrationEndDate,
                    IsActive = o.IsActive,
                    Notes = o.Notes,
                    TotalClasses = o.Classes?.Count ?? 0,
                    TotalStudents = o.Classes?.Sum(c => c.Members?.Count ?? 0) ?? 0
                }).ToList(),
                TotalOfferings = subject.Offerings.Count,
                TotalClasses = subject.Offerings.Sum(o => o.Classes?.Count ?? 0),
                TotalStudentsEnrolled = subject.Offerings.Sum(o => o.Classes?.Sum(c => c.Members?.Count ?? 0) ?? 0),
                Specializations = MapSpecializations(subject)
            };
        }

        public async Task<(bool Success, string Message, Guid? SubjectId)> CreateSubjectAsync(CreateSubjectRequest request)
        {
            try
            {
                var specializationIds = NormalizeIds(request.SpecializationIds);
                var (isValid, validationMessage) = await ValidateSpecializationIdsAsync(specializationIds);
                if (!isValid)
                {
                    return (false, validationMessage ?? "Invalid specializations", null);
                }

                // Check if subject code already exists
                var existingSubject = await _uow.Subjects.GetBySubjectCodeAsync(request.SubjectCode);
                if (existingSubject != null)
                {
                    return (false, $"Subject with code '{request.SubjectCode}' already exists", null);
                }

                var subject = new Subject
                {
                    Id = Guid.NewGuid(),
                    SubjectCode = request.SubjectCode,
                    SubjectName = request.SubjectName,
                    Credits = request.Credits,
                    Description = request.Description,
                    Category = request.Category,
                    Department = request.Department,
                    Prerequisites = request.Prerequisites,
                    IsActive = true
                };

                await _uow.Subjects.AddAsync(subject);
                await _uow.SaveChangesAsync();

                await AssignSubjectSpecializationsAsync(subject.Id, specializationIds);
                await _uow.SaveChangesAsync();

                var activeSemesters = await _uow.Semesters.FindAsync(s => s.IsActive && !s.IsClosed);
                var subjectOfferingsCreated = 0;

                foreach (var semester in activeSemesters)
                {
                    var subjectOffering = new SubjectOffering
                    {
                        Id = Guid.NewGuid(),
                        SubjectId = subject.Id,
                        SemesterId = semester.Id,
                        MaxClasses = 10,
                        SemesterCapacity = 400,
                        RegistrationStartDate = semester.StartDate.AddDays(-14),
                        RegistrationEndDate = semester.StartDate.AddDays(7),
                        IsActive = true,
                        Notes = $"Auto-created for new subject {subject.SubjectCode}"
                    };

                    await _uow.SubjectOfferings.AddAsync(subjectOffering);
                    subjectOfferingsCreated++;
                }

                await _uow.SaveChangesAsync();

                _logger.LogInformation("Subject created: {SubjectCode}. Auto-created {Count} subject offerings for active semesters.",
                    subject.SubjectCode, subjectOfferingsCreated);
                
                return (true, $"Subject created successfully with {subjectOfferingsCreated} subject offerings for active semesters.", subject.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subject");
                return (false, "An error occurred while creating the subject", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request)
        {
            try
            {
                var specializationIds = NormalizeIds(request.SpecializationIds);
                var (isValid, validationMessage) = await ValidateSpecializationIdsAsync(specializationIds);
                if (!isValid)
                {
                    return (false, validationMessage ?? "Invalid specializations");
                }

                var subject = await _uow.Subjects.GetByIdAsync(id);
                if (subject == null)
                {
                    return (false, "Subject not found");
                }

                // Check if new subject code conflicts with another subject
                if (subject.SubjectCode != request.SubjectCode)
                {
                    var existingSubject = await _uow.Subjects.GetBySubjectCodeAsync(request.SubjectCode);
                    if (existingSubject != null)
                    {
                        return (false, $"Subject with code '{request.SubjectCode}' already exists");
                    }
                }

                subject.SubjectCode = request.SubjectCode;
                subject.SubjectName = request.SubjectName;
                subject.Credits = request.Credits;
                subject.Description = request.Description;
                subject.Category = request.Category;
                subject.Department = request.Department;
                subject.Prerequisites = request.Prerequisites;

                _uow.Subjects.Update(subject);
                await AssignSubjectSpecializationsAsync(subject.Id, specializationIds);
                await _uow.SaveChangesAsync();

                _logger.LogInformation($"Subject updated: {subject.SubjectCode}");
                return (true, "Subject updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating subject: {ex.Message}");
                return (false, "An error occurred while updating the subject");
            }
        }

        public async Task<(bool Success, string Message)> DeleteSubjectAsync(Guid id)
        {
            try
            {
                var subject = await _uow.Subjects.GetByIdWithDetailsAsync(id);
                if (subject == null)
                {
                    return (false, "Subject not found");
                }

                if (subject.Offerings != null && subject.Offerings.Any(o => o.Classes != null && o.Classes.Any()))
                {
                    return (false, "Cannot delete subject that has existing classes in any semester");
                }

                // Check if any offering is in a closed semester
                if (subject.Offerings != null && subject.Offerings.Any(o => o.Semester != null && o.Semester.IsClosed))
                {
                    return (false, "Cannot delete subjects that have offerings in closed semesters");
                }

                _uow.Subjects.Remove(subject);
                await _uow.SaveChangesAsync();

                _logger.LogInformation($"Subject deleted: {subject.SubjectCode}");
                return (true, "Subject deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting subject: {ex.Message}");
                return (false, "An error occurred while deleting the subject");
            }
        }

        private async Task AssignSubjectSpecializationsAsync(Guid subjectId, List<Guid> specializationIds)
        {
            await _uow.Subjects.SetSpecializationsAsync(subjectId, specializationIds ?? new List<Guid>());
        }

        private async Task<(bool Success, string? Message)> ValidateSpecializationIdsAsync(List<Guid> specializationIds)
        {
            if (!specializationIds.Any())
            {
                return (true, null);
            }

            var existing = await _uow.Specializations.GetByIdsAsync(specializationIds);
            if (existing.Count() != specializationIds.Count)
            {
                return (false, "One or more specialization IDs are invalid");
            }

            return (true, null);
        }

        private static List<Guid> NormalizeIds(IEnumerable<Guid> specializationIds)
        {
            return specializationIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>();
        }

        private static List<SpecializationDto> MapSpecializations(Subject subject)
        {
            return subject.SubjectSpecializations?
                .Select(ss => new SpecializationDto
                {
                    Id = ss.SpecializationId,
                    Code = ss.Specialization?.Code ?? string.Empty,
                    Name = ss.Specialization?.Name ?? string.Empty,
                    Description = ss.Specialization?.Description,
                    IsActive = ss.Specialization?.IsActive ?? true
                })
                .OrderBy(s => s.Name)
                .ToList() ?? new List<SpecializationDto>();
        }
    }
}
