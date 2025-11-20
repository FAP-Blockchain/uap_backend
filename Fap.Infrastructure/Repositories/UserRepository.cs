using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(FapDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Student)  // Load Student data
                .Include(u => u.Teacher) // Load Teacher data
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        
        public async Task<(List<User> Users, int TotalCount)> GetPagedUsersAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? roleName,
            bool? isActive,
            string? sortBy,
            string? sortOrder)
        {
            var query = _dbSet
                .Include(u => u.Role)
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                query = query.Where(u => u.Role.Name.ToLower() == roleName.ToLower());
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            query = ApplySorting(query, sortBy, sortOrder);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        private IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "fullname" => isDescending
                    ? query.OrderByDescending(u => u.FullName)
                    : query.OrderBy(u => u.FullName),
                "email" => isDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "createdat" => isDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                "role" => isDescending
                    ? query.OrderByDescending(u => u.Role.Name)
                    : query.OrderBy(u => u.Role.Name),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };
        }
    }
}
