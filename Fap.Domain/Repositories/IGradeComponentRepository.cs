using Fap.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IGradeComponentRepository : IGenericRepository<GradeComponent>
    {
        Task<GradeComponent?> GetByIdWithGradesAsync(Guid id);
        
        Task<GradeComponent?> GetByNameAsync(string name);
   
        Task<List<GradeComponent>> GetAllWithGradeCountAsync();
      
        Task<bool> IsComponentInUseAsync(Guid componentId);
    }
}
