using System;
using System.Collections.Generic;
using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface ITeacherRepository : IGenericRepository<Teacher>
    {
        Task<Teacher?> GetByTeacherCodeAsync(string teacherCode);
        Task<Teacher?> GetByUserIdAsync(Guid userId);
        Task<Teacher?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Teacher>> GetAllWithUsersAsync();
        Task<(List<Teacher> Teachers, int TotalCount)> GetPagedTeachersAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? specializationKeyword,
            Guid? specializationId,
            bool? isActive,
            string? sortBy,
            string? sortOrder
        );
        Task<List<Guid>> GetSpecializationIdsAsync(Guid teacherId);
        Task SetSpecializationsAsync(Guid teacherId, IEnumerable<Guid> specializationIds);
    }
}