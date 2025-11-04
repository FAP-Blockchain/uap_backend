using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
        }

        public async Task<Role?> GetByIdWithPermissionsAsync(Guid roleId)
        {
            return await _dbSet
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task<Role?> GetByIdWithUsersAsync(Guid roleId)
        {
            return await _dbSet
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task<IEnumerable<Role>> GetAllWithPermissionsAsync()
        {
            return await _dbSet
                .Include(r => r.Permissions)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetAllWithUserCountAsync()
        {
            return await _dbSet
                .Include(r => r.Users)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}