using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Role
{
    public class CreateRoleRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class UpdateRoleRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class AssignPermissionsRequest
    {
        [Required]
        public List<PermissionItem> Permissions { get; set; } = new();
    }

    public class PermissionItem
    {
        [Required, MaxLength(150)]
        public string Code { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }
    }
}
