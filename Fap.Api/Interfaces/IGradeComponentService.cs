using Fap.Domain.DTOs.GradeComponent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IGradeComponentService
    {
        Task<List<GradeComponentDto>> GetAllGradeComponentsAsync();

        Task<GradeComponentDto?> GetGradeComponentByIdAsync(Guid id);

        Task<GradeComponentResponse> CreateGradeComponentAsync(CreateGradeComponentRequest request);
    
        Task<GradeComponentResponse> UpdateGradeComponentAsync(Guid id, UpdateGradeComponentRequest request);

        Task<GradeComponentResponse> DeleteGradeComponentAsync(Guid id);
    }
}
