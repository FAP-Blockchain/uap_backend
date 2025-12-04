using System;
using System.Collections.Generic;
using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface ISpecializationRepository : IGenericRepository<Specialization>
    {
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);
        Task<IReadOnlyList<Specialization>> GetActiveAsync();
    }
}
