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

        public async Task<List<GradeComponentDto>> GetAllGradeComponentsAsync(Guid? subjectId = null)
        {
            try
            {
                var components = await _uow.GradeComponents.GetAllWithGradeCountAsync();
                
                // Filter by SubjectId if provided
                if (subjectId.HasValue)
                {
                    components = components.Where(gc => gc.SubjectId == subjectId.Value).ToList();
                }
                
                return components.Select(gc => new GradeComponentDto
                {
                    Id = gc.Id,
                    Name = gc.Name,
                    WeightPercent = gc.WeightPercent,
                    SubjectId = gc.SubjectId,
                    SubjectCode = gc.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = gc.Subject?.SubjectName ?? string.Empty,
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
                    SubjectId = component.SubjectId,
                    SubjectCode = component.Subject?.SubjectCode ?? string.Empty,
                    SubjectName = component.Subject?.SubjectName ?? string.Empty,
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
                // Validate SubjectId exists
                var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    response.Errors.Add($"Subject with ID '{request.SubjectId}' not found");
                    response.Message = "Grade component creation failed";
                    return response;
                }

                // Check if component with same name already exists for this subject
                var existingComponents = await _uow.GradeComponents.GetAllWithGradeCountAsync();
                var duplicate = existingComponents.FirstOrDefault(gc => 
                    gc.SubjectId == request.SubjectId && 
                    gc.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
                    
                if (duplicate != null)
                {
                    response.Errors.Add($"Grade component '{request.Name}' already exists for subject '{subject.SubjectCode}'");
                    response.Message = "Grade component creation failed";
                    return response;
                }

                var newComponent = new GradeComponent
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    WeightPercent = request.WeightPercent,
                    SubjectId = request.SubjectId
                };

                await _uow.GradeComponents.AddAsync(newComponent);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Grade component created successfully";
                response.GradeComponentId = newComponent.Id;

                _logger.LogInformation("Grade component created: {ComponentName} for Subject {SubjectId}", request.Name, request.SubjectId);

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

                if (component.SubjectId != request.SubjectId)
                {
                    response.Errors.Add("Updating the subject of an existing grade component is not supported. Use the bulk configuration endpoint to restructure components.");
                    response.Message = "Grade component update failed";
                    return response;
                }

                // Validate SubjectId exists
                var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    response.Errors.Add($"Subject with ID '{request.SubjectId}' not found");
                    response.Message = "Grade component update failed";
                    return response;
                }

                // Load subject component graph for validation
                var subjectComponents = await _uow.GradeComponents.GetBySubjectWithGradesAsync(component.SubjectId);
                var componentSnapshot = subjectComponents.FirstOrDefault(gc => gc.Id == id);
                if (componentSnapshot == null)
                {
                    response.Errors.Add("Unable to load grade component details for validation");
                    response.Message = "Grade component update failed";
                    return response;
                }

                var componentLookup = subjectComponents.ToDictionary(gc => gc.Id, gc => gc);
                var childrenLookup = subjectComponents
                    .GroupBy(gc => gc.ParentId ?? Guid.Empty)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Prevent edits when grades already recorded for this component or its descendants
                if (HasRecordedGradesInTree(componentSnapshot.Id, componentLookup, childrenLookup))
                {
                    response.Errors.Add("Cannot update this grade component because grades have already been recorded for it or its sub components.");
                    response.Message = "Grade component update failed";
                    return response;
                }

                // Check if another component with same name exists for this subject
                var duplicate = subjectComponents.FirstOrDefault(gc =>
                    gc.SubjectId == request.SubjectId &&
                    gc.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase) &&
                    gc.Id != id);
                    
                if (duplicate != null)
                {
                    response.Errors.Add($"Grade component '{request.Name}' already exists for subject '{subject.SubjectCode}'");
                    response.Message = "Grade component update failed";
                    return response;
                }

                try
                {
                    ValidateWeightsForUpdate(componentSnapshot, request.WeightPercent, componentLookup, childrenLookup);
                }
                catch (InvalidOperationException ex)
                {
                    response.Errors.Add(ex.Message);
                    response.Message = "Grade component update failed";
                    return response;
                }

                component.Name = request.Name;
                component.WeightPercent = request.WeightPercent;
                component.SubjectId = request.SubjectId;

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
        public async Task<List<GradeComponentDto>> CreateSubjectGradeComponentsAsync(CreateSubjectGradeComponentsRequest request)
        {
            // 1. Validate Subject
            var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
            if (subject == null)
            {
                throw new InvalidOperationException($"Subject with ID '{request.SubjectId}' not found");
            }

            if (request.Components == null || !request.Components.Any())
            {
                throw new InvalidOperationException("At least one grade component is required when configuring a subject.");
            }

            // 2. Validate Total Weight of Top-Level Components and all descendants
            ValidateComponentTree(
                request.Components,
                expectedTotalWeight: 100,
                context: $"subject '{subject.SubjectCode ?? subject.SubjectName}' components");

            // 3. Clear existing components for this subject (Optional: or update them)
            // For simplicity, we might want to remove old ones and add new ones, 
            // BUT we must check if grades exist. If grades exist, we can't just delete.
            // Assuming this is for setting up a NEW subject or resetting one without grades.
            var existingComponents = (await _uow.GradeComponents.GetAllWithGradeCountAsync())
                                    .Where(gc => gc.SubjectId == request.SubjectId)
                                    .ToList();

            if (existingComponents.Any(gc => gc.Grades != null && gc.Grades.Any()))
            {
                 throw new InvalidOperationException("Cannot reset grade components because grades have already been recorded for this subject.");
            }

            // Remove existing
            foreach (var existing in existingComponents)
            {
                _uow.GradeComponents.Remove(existing);
            }

            // 4. Create new components recursively
            var createdDtos = new List<GradeComponentDto>();

            foreach (var compDto in request.Components)
            {
                var created = await CreateGradeComponentRecursiveAsync(compDto, request.SubjectId, parentId: null);
                createdDtos.Add(created);
            }

            await _uow.SaveChangesAsync();
            return createdDtos;
        }

        public async Task<List<GradeComponentDto>> GetSubjectGradeComponentTreeAsync(Guid subjectId)
        {
            var subject = await _uow.Subjects.GetByIdAsync(subjectId);
            if (subject == null)
            {
                throw new InvalidOperationException($"Subject with ID '{subjectId}' not found");
            }

            var components = await _uow.GradeComponents.GetBySubjectWithGradesAsync(subjectId);

            if (components == null || !components.Any())
            {
                return new List<GradeComponentDto>();
            }

            var dtoLookup = components.ToDictionary(
                gc => gc.Id,
                gc => MapGradeComponentToDto(gc));

            foreach (var dto in dtoLookup.Values)
            {
                dto.SubComponents.Clear();
            }

            var roots = new List<GradeComponentDto>();

            foreach (var component in components)
            {
                var dto = dtoLookup[component.Id];

                if (component.ParentId.HasValue && dtoLookup.TryGetValue(component.ParentId.Value, out var parentDto))
                {
                    parentDto.SubComponents.Add(dto);
                }
                else
                {
                    roots.Add(dto);
                }
            }

            return roots.OrderBy(r => r.Name).ToList();
        }

        private static GradeComponentDto MapGradeComponentToDto(GradeComponent component)
        {
            return new GradeComponentDto
            {
                Id = component.Id,
                Name = component.Name,
                WeightPercent = component.WeightPercent,
                SubjectId = component.SubjectId,
                SubjectCode = component.Subject?.SubjectCode ?? string.Empty,
                SubjectName = component.Subject?.SubjectName ?? string.Empty,
                GradeCount = component.Grades?.Count ?? 0,
                ParentId = component.ParentId,
                SubComponents = new List<GradeComponentDto>()
            };
        }

        private static void ValidateComponentTree(
            IEnumerable<CreateGradeComponentDto> components,
            int expectedTotalWeight,
            string context)
        {
            var componentList = components?.ToList() ?? new List<CreateGradeComponentDto>();
            var totalWeight = componentList.Sum(c => c.WeightPercent);

            if (totalWeight != expectedTotalWeight)
            {
                var expectedText = expectedTotalWeight == 100
                    ? "100%"
                    : $"{expectedTotalWeight}%";

                throw new InvalidOperationException(
                    $"Weight verification failed for {context}. Expected {expectedText} but found {totalWeight}%.");
            }

            foreach (var component in componentList)
            {
                if (component.SubComponents != null && component.SubComponents.Any())
                {
                    ValidateComponentTree(
                        component.SubComponents,
                        component.WeightPercent,
                        $"component '{component.Name}'");
                }
            }
        }

        private async Task<GradeComponentDto> CreateGradeComponentRecursiveAsync(
            CreateGradeComponentDto dto,
            Guid subjectId,
            Guid? parentId)
        {
            var entity = new GradeComponent
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                WeightPercent = dto.WeightPercent,
                SubjectId = subjectId,
                ParentId = parentId
            };

            await _uow.GradeComponents.AddAsync(entity);

            var response = new GradeComponentDto
            {
                Id = entity.Id,
                Name = entity.Name,
                WeightPercent = entity.WeightPercent,
                SubjectId = entity.SubjectId,
                ParentId = entity.ParentId,
                SubComponents = new List<GradeComponentDto>()
            };

            if (dto.SubComponents != null && dto.SubComponents.Any())
            {
                foreach (var child in dto.SubComponents)
                {
                    var childResponse = await CreateGradeComponentRecursiveAsync(child, subjectId, entity.Id);
                    response.SubComponents.Add(childResponse);
                }
            }

            return response;
        }

        private static bool HasRecordedGradesInTree(
            Guid componentId,
            Dictionary<Guid, GradeComponent> componentLookup,
            Dictionary<Guid, List<GradeComponent>> childrenLookup)
        {
            var stack = new Stack<Guid>();
            stack.Push(componentId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();

                if (componentLookup.TryGetValue(currentId, out var component) &&
                    component.Grades?.Any() == true)
                {
                    return true;
                }

                if (childrenLookup.TryGetValue(currentId, out var children))
                {
                    foreach (var child in children)
                    {
                        stack.Push(child.Id);
                    }
                }
            }

            return false;
        }

        private static void ValidateWeightsForUpdate(
            GradeComponent componentSnapshot,
            int requestedWeight,
            Dictionary<Guid, GradeComponent> componentLookup,
            Dictionary<Guid, List<GradeComponent>> childrenLookup)
        {
            var parentId = componentSnapshot.ParentId;
            var siblingsKey = parentId ?? Guid.Empty;
            var siblings = childrenLookup.TryGetValue(siblingsKey, out var siblingList)
                ? siblingList
                : new List<GradeComponent>();

            var expectedTotal = parentId.HasValue
                ? componentLookup.TryGetValue(parentId.Value, out var parent)
                    ? parent.WeightPercent
                    : throw new InvalidOperationException("Parent grade component could not be found while validating weights.")
                : 100;

            var siblingTotal = siblings.Sum(gc => gc.Id == componentSnapshot.Id ? requestedWeight : gc.WeightPercent);

            if (siblingTotal != expectedTotal)
            {
                var context = parentId.HasValue
                    ? $"children of '{componentLookup[parentId.Value].Name}'"
                    : "top-level components";

                throw new InvalidOperationException(
                    $"Weight verification failed for {context}. Expected {expectedTotal}% but found {siblingTotal}%.");
            }

            if (childrenLookup.TryGetValue(componentSnapshot.Id, out var childComponents) && childComponents.Any())
            {
                var childTotal = childComponents.Sum(c => c.WeightPercent);
                if (childTotal != requestedWeight)
                {
                    throw new InvalidOperationException(
                        $"Weight verification failed for component '{componentSnapshot.Name}'. Its sub components sum to {childTotal}%, so the component weight must also be {childTotal}%.");
                }
            }
        }
    }
}
