using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key] public Guid Id { get; set; }
        [Required] public string Token { get; set; }
        [Required] public DateTime Expires { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; }

        [NotMapped] public bool IsExpired => DateTime.UtcNow >= Expires;
    }
}
