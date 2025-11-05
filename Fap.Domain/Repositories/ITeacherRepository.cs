using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface ITeacherRepository : IGenericRepository<Teacher>
    {
        Task<Teacher?> GetByTeacherCodeAsync(string teacherCode);
        Task<Teacher?> GetByUserIdAsync(Guid userId);
        Task<Teacher?> GetByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Teacher>> GetAllWithUsersAsync();
    }
}