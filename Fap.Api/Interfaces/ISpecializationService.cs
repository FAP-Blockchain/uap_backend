using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fap.Domain.DTOs.Specialization;

namespace Fap.Api.Interfaces
{
    public interface ISpecializationService
    {
        Task<List<SpecializationDto>> GetAllAsync();
        Task<SpecializationDetailDto?> GetByIdAsync(Guid id);
        Task<SpecializationDto> CreateAsync(CreateSpecializationRequest request);
        Task<bool> UpdateAsync(Guid id, UpdateSpecializationRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}
