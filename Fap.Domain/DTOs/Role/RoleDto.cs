using System;
using System.Collections.Generic;

namespace Fap.Domain.DTOs.Role
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int UserCount { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }
}
