using Fap.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]  // ? Ch? Admin m?i có quy?n xem permissions
    public class PermissionsController : ControllerBase
    {
        private readonly RoleService _roleService;

        public PermissionsController(RoleService roleService)
        {
            _roleService = roleService;
        }

        // ========================================
        // ?? API #11: GET /api/permissions
        // L?y danh sách t?t c? permissions
        // ========================================
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _roleService.GetAllPermissionsAsync();

            return Ok(new
            {
                success = true,
                data = permissions,
                count = permissions.Count
            });
        }

        // ========================================
        // ?? GET /api/permissions/role/{roleId}
        // L?y permissions theo RoleId (Bonus API)
        // ========================================
        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetPermissionsByRoleId(Guid roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            
            if (role == null)
                return NotFound(new { message = $"Role with ID '{roleId}' not found" });

            var permissions = await _roleService.GetPermissionsByRoleIdAsync(roleId);

            return Ok(new
            {
                success = true,
                roleId = role.Id,
                roleName = role.Name,
                permissions = permissions,
                count = permissions.Count
            });
        }
    }
}
