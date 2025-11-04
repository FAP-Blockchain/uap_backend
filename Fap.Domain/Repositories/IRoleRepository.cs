using Fap.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName);
        Task<Role?> GetByIdWithPermissionsAsync(Guid roleId);
        Task<Role?> GetByIdWithUsersAsync(Guid roleId);
        Task<IEnumerable<Role>> GetAllWithPermissionsAsync();
        Task<IEnumerable<Role>> GetAllWithUserCountAsync();
    }
}