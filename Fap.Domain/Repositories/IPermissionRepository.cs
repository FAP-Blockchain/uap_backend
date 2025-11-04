using Fap.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IPermissionRepository : IGenericRepository<Permission>
    {
        Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId);
        Task<Permission?> GetByCodeAsync(string code);
        Task DeleteByRoleIdAsync(Guid roleId);
    }
}
