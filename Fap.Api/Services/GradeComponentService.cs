using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.GradeComponent;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class GradeComponentService : IGradeComponentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GradeComponentService> _logger;

        public GradeComponentService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<GradeComponentService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<GradeComponentDto>> GetAllGradeComponentsAsync()
        {
            try
            {
                var components = await _uow.GradeComponents.GetAllWithGradeCountAsync();
                return components.Select(gc => new GradeComponentDto
                {
                    Id = gc.Id,
                    Name = gc.Name,
                    WeightPercent = gc.WeightPercent,
                    GradeCount = gc.Grades?.Count ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all grade components");
                return new List<GradeComponentDto>();
            }
        }

        public async Task<GradeComponentDto?> GetGradeComponentByIdAsync(Guid id)
        {
            try
            {
                var component = await _uow.GradeComponents.GetByIdWithGradesAsync(id);
                if (component == null) return null;

                return new GradeComponentDto
                {
                    Id = component.Id,
                    Name = component.Name,
                    WeightPercent = component.WeightPercent,
                    GradeCount = component.Grades?.Count ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grade component {ComponentId}", id);
                return null;
            }
        }

        public async Task<GradeComponentResponse> CreateGradeComponentAsync(CreateGradeComponentRequest request)
        {
            var response = new GradeComponentResponse();

            try
            {
                // Check if component with same name already exists
                var existingComponent = await _uow.GradeComponents.GetByNameAsync(request.Name);
                if (existingComponent != null)
                {
                    response.Errors.Add($"Grade component with name '{request.Name}' already exists");
                    response.Message = "Grade component creation failed";
                    return response;
                }

                var newComponent = new GradeComponent
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    WeightPercent = request.WeightPercent
                };

                await _uow.GradeComponents.AddAsync(newComponent);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade component created successfully";
                response.GradeComponentId = newComponent.Id;

                _logger.LogInformation("Grade component created: {ComponentName}", request.Name);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating grade component");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Grade component creation failed";
                return response;
            }
        }

        public async Task<GradeComponentResponse> UpdateGradeComponentAsync(
            Guid id,
            UpdateGradeComponentRequest request)
        {
            var response = new GradeComponentResponse();

            try
            {
                var component = await _uow.GradeComponents.GetByIdAsync(id);
                if (component == null)
                {
                    response.Errors.Add($"Grade component with ID '{id}' not found");
                    response.Message = "Grade component update failed";
                    return response;
                }

                // Check if another component with same name exists
                var existingComponent = await _uow.GradeComponents.GetByNameAsync(request.Name);
                if (existingComponent != null && existingComponent.Id != id)
                {
                    response.Errors.Add($"Grade component with name '{request.Name}' already exists");
                    response.Message = "Grade component update failed";
                    return response;
                }

                component.Name = request.Name;
                component.WeightPercent = request.WeightPercent;

                _uow.GradeComponents.Update(component);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade component updated successfully";
                response.GradeComponentId = component.Id;

                _logger.LogInformation("Grade component {ComponentId} updated", id);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grade component {ComponentId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Grade component update failed";
                return response;
            }
        }

        public async Task<GradeComponentResponse> DeleteGradeComponentAsync(Guid id)
        {
            var response = new GradeComponentResponse();

            try
            {
                var component = await _uow.GradeComponents.GetByIdAsync(id);
                if (component == null)
                {
                    response.Errors.Add($"Grade component with ID '{id}' not found");
                    response.Message = "Grade component deletion failed";
                    return response;
                }

                // Check if component is in use
                var isInUse = await _uow.GradeComponents.IsComponentInUseAsync(id);
                if (isInUse)
                {
                    response.Errors.Add("Cannot delete grade component that is currently in use");
                    response.Message = "Grade component deletion failed";
                    return response;
                }

                _uow.GradeComponents.Remove(component);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade component deleted successfully";

                _logger.LogInformation("Grade component {ComponentId} deleted", id);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting grade component {ComponentId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Grade component deletion failed";
                return response;
            }
        }
    }
}
