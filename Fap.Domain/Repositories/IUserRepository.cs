using Fap.Domain.Entities;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithRoleAsync(Guid id);
        Task<User?> GetByIdWithDetailsAsync(Guid id); 
        Task<(List<User> Users, int TotalCount)> GetPagedUsersAsync(
            int page, 
            int pageSize, 
            string? searchTerm, 
            string? roleName, 
            bool? isActive,
            string? sortBy,
            string? sortOrder
        ); 
    }
}
