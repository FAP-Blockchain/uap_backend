using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Permissions")]
    public class Permission
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(150)] public string Code { get; set; }
        [MaxLength(250)] public string Description { get; set; }

        [Required] public Guid RoleId { get; set; }
        [ForeignKey(nameof(RoleId))] public Role Role { get; set; }
    }
}
