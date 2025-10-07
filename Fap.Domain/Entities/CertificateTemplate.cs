using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("CertificateTemplates")]
    public class CertificateTemplate
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(120)] public string Name { get; set; }
        [MaxLength(500)] public string Description { get; set; }
        [MaxLength(200)] public string StorageUri { get; set; } // IPFS/Cloud template

        public virtual ICollection<Credential> Credentials { get; set; }
    }
}
