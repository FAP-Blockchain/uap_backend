using AutoMapper;
using Fap.Domain.DTOs.Role;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class RoleService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public RoleService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        // ========== GET ALL ROLES WITH PERMISSIONS ==========
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _uow.Roles.GetAllWithUserCountAsync();
            
            var roleDtos = new List<RoleDto>();
            foreach (var role in roles)
            {
                var permissions = await _uow.Permissions.GetByRoleIdAsync(role.Id);
                
                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    UserCount = role.Users?.Count ?? 0,
                    Permissions = permissions.Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Description = p.Description
                    }).ToList()
                };
                
                roleDtos.Add(roleDto);
            }

            return roleDtos;
        }

        // ========== GET ROLE BY ID WITH PERMISSIONS ==========
        public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
        {
            var role = await _uow.Roles.GetByIdWithUsersAsync(roleId);
            if (role == null) return null;

            var permissions = await _uow.Permissions.GetByRoleIdAsync(roleId);

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                UserCount = role.Users?.Count ?? 0,
                Permissions = permissions.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Description = p.Description
                }).ToList()
            };
        }

        // ========== CREATE ROLE ==========
        public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request)
        {
            var response = new RoleResponse
            {
                RoleName = request.Name
            };

            try
            {
                // 1?? Validate role name không trùng
                var existingRole = await _uow.Roles.GetByNameAsync(request.Name);
                if (existingRole != null)
                {
                    response.Errors.Add($"Role '{request.Name}' already exists");
                    response.Message = "Role creation failed";
                    return response;
                }

                // 2?? T?o role m?i
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name
                };

                await _uow.Roles.AddAsync(role);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role created successfully";
                response.RoleId = role.Id;

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Role creation failed";
                return response;
            }
        }

        // ========== UPDATE ROLE ==========
        public async Task<RoleResponse> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
        {
            var response = new RoleResponse
            {
                RoleId = roleId,
                RoleName = request.Name
            };

            try
            {
                // 1?? Ki?m tra role t?n t?i
                var role = await _uow.Roles.GetByIdAsync(roleId);
                if (role == null)
                {
                    response.Errors.Add($"Role with ID '{roleId}' not found");
                    response.Message = "Role update failed";
                    return response;
                }

                // 2?? Validate tên không trùng v?i role khác
                var existingRole = await _uow.Roles.GetByNameAsync(request.Name);
                if (existingRole != null && existingRole.Id != roleId)
                {
                    response.Errors.Add($"Role '{request.Name}' already exists");
                    response.Message = "Role update failed";
                    return response;
                }

                // 3?? C?p nh?t role
                role.Name = request.Name;
                _uow.Roles.Update(role);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role updated successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Role update failed";
                return response;
            }
        }

        // ========== ASSIGN PERMISSIONS TO ROLE (ADD/MERGE STRATEGY) ==========
        public async Task<AssignPermissionsResponse> AssignPermissionsAsync(Guid roleId, AssignPermissionsRequest request)
        {
            var response = new AssignPermissionsResponse
            {
                RoleId = roleId
            };

            try
            {
                // 1?? Ki?m tra role t?n t?i
                var role = await _uow.Roles.GetByIdAsync(roleId);
                if (role == null)
                {
                    response.Errors.Add($"Role with ID '{roleId}' not found");
                    response.Message = "Permission assignment failed";
                    return response;
                }

                response.RoleName = role.Name;

                // 2?? L?y danh sách permissions hi?n t?i c?a role
                var existingPermissions = await _uow.Permissions.GetByRoleIdAsync(roleId);
                var existingCodes = existingPermissions.Select(p => p.Code.ToLower()).ToHashSet();

                // 3?? L?c ra các permissions m?i (ch?a t?n t?i)
                var newPermissions = request.Permissions
                    .Where(p => !existingCodes.Contains(p.Code.ToLower()))
                    .Select(p => new Permission
                    {
                        Id = Guid.NewGuid(),
                        Code = p.Code,
                        Description = p.Description ?? string.Empty,
                        RoleId = roleId
                    })
                    .ToList();

                // 4?? Ki?m tra có permissions m?i không
                if (newPermissions.Count == 0)
                {
                    response.Success = true;
                    response.Message = "No new permissions to add (all already exist)";
                    response.PermissionsAssigned = 0;
                    return response;
                }

                // 5?? Thêm permissions m?i
                foreach (var permission in newPermissions)
                {
                    await _uow.Permissions.AddAsync(permission);
                }

                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Successfully added {newPermissions.Count} new permission(s)";
                response.PermissionsAssigned = newPermissions.Count;

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Permission assignment failed";
                return response;
            }
        }

        // ========== DELETE ROLE ==========
        public async Task<RoleResponse> DeleteRoleAsync(Guid roleId)
        {
            var response = new RoleResponse
            {
                RoleId = roleId
            };

            try
            {
                // 1?? Ki?m tra role t?n t?i
                var role = await _uow.Roles.GetByIdWithUsersAsync(roleId);
                if (role == null)
                {
                    response.Errors.Add($"Role with ID '{roleId}' not found");
                    response.Message = "Role deletion failed";
                    return response;
                }

                response.RoleName = role.Name;

                // 2?? Ki?m tra xem có user nào ?ang s? d?ng role không
                if (role.Users != null && role.Users.Any())
                {
                    response.Errors.Add($"Cannot delete role '{role.Name}' because it has {role.Users.Count} users");
                    response.Message = "Role deletion failed";
                    return response;
                }

                // 3?? Xóa permissions c?a role tr??c
                await _uow.Permissions.DeleteByRoleIdAsync(roleId);

                // 4?? Xóa role
                _uow.Roles.Remove(role);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Role deleted successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Role deletion failed";
                return response;
            }
        }

        // ========== GET ALL PERMISSIONS ==========
        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _uow.Permissions.GetAllAsync();

            return permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description
            }).OrderBy(p => p.Code).ToList();
        }

        // ========== GET PERMISSIONS BY ROLE ==========
        public async Task<List<PermissionDto>> GetPermissionsByRoleIdAsync(Guid roleId)
        {
            var permissions = await _uow.Permissions.GetByRoleIdAsync(roleId);

            return permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description
            }).ToList();
        }

        // ========== REMOVE SPECIFIC PERMISSION FROM ROLE ==========
        public async Task<RoleResponse> RemovePermissionAsync(Guid roleId, Guid permissionId)
        {
            var response = new RoleResponse
            {
                RoleId = roleId
            };

            try
            {
                // 1?? Ki?m tra role t?n t?i
                var role = await _uow.Roles.GetByIdAsync(roleId);
                if (role == null)
                {
                    response.Errors.Add($"Role with ID '{roleId}' not found");
                    response.Message = "Permission removal failed";
                    return response;
                }

                response.RoleName = role.Name;

                // 2?? Tìm permission
                var permission = await _uow.Permissions.GetByIdAsync(permissionId);
                if (permission == null)
                {
                    response.Errors.Add($"Permission with ID '{permissionId}' not found");
                    response.Message = "Permission removal failed";
                    return response;
                }

                // 3?? Ki?m tra permission có thu?c role không
                if (permission.RoleId != roleId)
                {
                    response.Errors.Add($"Permission does not belong to role '{role.Name}'");
                    response.Message = "Permission removal failed";
                    return response;
                }

                // 4?? Xóa permission
                _uow.Permissions.Remove(permission);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Permission '{permission.Code}' removed successfully from role '{role.Name}'";

                return response;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Permission removal failed";
                return response;
            }
        }
    }
}
