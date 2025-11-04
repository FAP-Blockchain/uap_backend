using Fap.Api.Services;
using Fap.Domain.DTOs.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly RoleService _roleService;

        public RolesController(RoleService roleService)
        {
            _roleService = roleService;
        }

        // GET /api/roles
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(new
            {
                success = true,
                data = roles,
                count = roles.Count
            });
        }

        // GET /api/roles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            
            if (role == null)
                return NotFound(new { message = $"Role with ID '{id}' not found" });

            return Ok(new
            {
                success = true,
                data = role
            });
        }

        // POST /api/roles
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var result = await _roleService.CreateRoleAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(
                nameof(GetRoleById),
                new { id = result.RoleId },
                result
            );
        }

        // PUT /api/roles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
        {
            var result = await _roleService.UpdateRoleAsync(id, request);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Contains("not found")))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }

        // POST /api/roles/{id}/permissions
        [HttpPost("{id}/permissions")]
        public async Task<IActionResult> AssignPermissions(Guid id, [FromBody] AssignPermissionsRequest request)
        {
            var result = await _roleService.AssignPermissionsAsync(id, request);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Contains("not found")))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }

        // DELETE /api/roles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var result = await _roleService.DeleteRoleAsync(id);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Contains("not found")))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }

        // GET /api/roles/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetRolePermissions(Guid id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            
            if (role == null)
                return NotFound(new { message = $"Role with ID '{id}' not found" });

            return Ok(new
            {
                success = true,
                roleId = role.Id,
                roleName = role.Name,
                permissions = role.Permissions,
                count = role.Permissions.Count
            });
        }

        // DELETE /api/roles/{roleId}/permissions/{permissionId}
        [HttpDelete("{roleId}/permissions/{permissionId}")]
        public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId)
        {
            var result = await _roleService.RemovePermissionAsync(roleId, permissionId);

            if (!result.Success)
            {
                if (result.Errors.Any(e => e.Contains("not found")))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
