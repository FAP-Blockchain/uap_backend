using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface ISemesterRepository : IGenericRepository<Semester>
    {
        Task<Semester?> GetByIdWithDetailsAsync(Guid id);
   Task<IEnumerable<Semester>> GetAllWithDetailsAsync();
        Task<Semester?> GetByNameAsync(string name);
        Task<Semester?> GetCurrentSemesterAsync();
        Task<bool> HasOverlappingDatesAsync(DateTime startDate, DateTime endDate, Guid? excludeId = null);
  }
}
