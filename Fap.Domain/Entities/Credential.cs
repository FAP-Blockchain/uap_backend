using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Credentials")]
    public class Credential
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(120)] public string CredentialId { get; set; } // on-chain id
        [MaxLength(200)] public string IPFSHash { get; set; }
        [Required] public DateTime IssuedDate { get; set; }
        public bool IsRevoked { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; }

        [Required] public Guid CertificateTemplateId { get; set; }
        [ForeignKey(nameof(CertificateTemplateId))] public CertificateTemplate CertificateTemplate { get; set; }
    }
}
