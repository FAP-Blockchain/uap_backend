using System;
using System.Collections.Generic;

namespace Fap.Domain.DTOs.Role
{
    public class RoleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? RoleId { get; set; }
        public string RoleName { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class AssignPermissionsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public int PermissionsAssigned { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
