using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Specialization;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class SpecializationService : ISpecializationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<SpecializationService> _logger;

        public SpecializationService(IUnitOfWork uow, IMapper mapper, ILogger<SpecializationService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<SpecializationDto>> GetAllAsync()
        {
            var specializations = await _uow.Specializations.GetAllAsync();
            return _mapper.Map<List<SpecializationDto>>(specializations);
        }

        public async Task<SpecializationDetailDto?> GetByIdAsync(Guid id)
        {
            var specialization = await _uow.Specializations.GetByIdAsync(id);
            return _mapper.Map<SpecializationDetailDto>(specialization);
        }

        public async Task<SpecializationDto> CreateAsync(CreateSpecializationRequest request)
        {
            if (await _uow.Specializations.CodeExistsAsync(request.Code))
            {
                throw new InvalidOperationException($"Specialization with code '{request.Code}' already exists.");
            }

            var specialization = _mapper.Map<Specialization>(request);
            
            await _uow.Specializations.AddAsync(specialization);
            await _uow.SaveChangesAsync();

            return _mapper.Map<SpecializationDto>(specialization);
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateSpecializationRequest request)
        {
            var specialization = await _uow.Specializations.GetByIdAsync(id);
            if (specialization == null) return false;

            _mapper.Map(request, specialization);
            specialization.UpdatedAt = DateTime.UtcNow;

            _uow.Specializations.Update(specialization);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var specialization = await _uow.Specializations.GetByIdAsync(id);
            if (specialization == null) return false;

            // Check if used by teachers or subjects?
            // For now, let's just delete. Or maybe soft delete?
            // The entity has IsActive, maybe we should just set IsActive = false?
            // But the interface says DeleteAsync. Let's do hard delete for now or check if there are constraints.
            // If there are FK constraints, it will fail.
            
            _uow.Specializations.Remove(specialization);
            await _uow.SaveChangesAsync();

            return true;
        }
    }
}
