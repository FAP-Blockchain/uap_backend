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
    public class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId)
        {
            return await _dbSet
                .Where(p => p.RoleId == roleId)
                .OrderBy(p => p.Code)
                .ToListAsync();
        }

        public async Task<Permission?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower());
        }

        public async Task DeleteByRoleIdAsync(Guid roleId)
        {
            var permissions = await _dbSet
                .Where(p => p.RoleId == roleId)
                .ToListAsync();

            _dbSet.RemoveRange(permissions);
        }
    }
}
