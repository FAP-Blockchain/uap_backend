using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("ActionLogs")]
    public class ActionLog
    {
        [Key] public Guid Id { get; set; }
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required, MaxLength(100)] public string Action { get; set; } // e.g., ISSUE_CREDENTIAL
        [MaxLength(500)] public string Detail { get; set; }

        // Blockchain audit fields (optional)
        [MaxLength(66)] public string? TransactionHash { get; set; }
        public long? BlockNumber { get; set; }
        [MaxLength(200)] public string? EventName { get; set; }
        [MaxLength(42)] public string? TxFrom { get; set; }
        [MaxLength(42)] public string? TxTo { get; set; }
        [MaxLength(42)] public string? ContractAddress { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; }

        public Guid? CredentialId { get; set; }
        [ForeignKey(nameof(CredentialId))] public Credential Credential { get; set; }
    }
}
