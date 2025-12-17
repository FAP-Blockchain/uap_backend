using System;
using System.ComponentModel.DataAnnotations;

namespace Fap.Api.DTOs.Blockchain
{
    public class TxReceiptAuditRequest
    {
        [Required]
        [MinLength(66), MaxLength(66)]
        public string TxHash { get; set; } = string.Empty;

        /// <summary>
        /// Optional application-level action label (e.g. REGISTER_USER, ISSUE_CREDENTIAL).
        /// If omitted, ActionLogs will be stored as CHAIN_EVENT.
        /// </summary>
        [MaxLength(100)]
        public string? Action { get; set; }

        /// <summary>
        /// Optional domain linkage when the tx relates to a Credential entity.
        /// </summary>
        public Guid? CredentialId { get; set; }

        /// <summary>
        /// Optional extra metadata from caller (e.g. UI context).
        /// </summary>
        [MaxLength(500)]
        public string? Detail { get; set; }
    }
}
