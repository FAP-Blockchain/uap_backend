using Fap.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface ICurriculumRepository : IGenericRepository<Curriculum>
    {
        Task<Curriculum?> GetByIdAsync(int id);
        Task<Curriculum?> GetByCodeAsync(string code);
        Task<IEnumerable<Curriculum>> GetAllWithDetailsAsync();
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}
