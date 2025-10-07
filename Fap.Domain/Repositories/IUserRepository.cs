using Fap.Domain.Entities;

namespace Fap.Domain.Repositories;
public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}