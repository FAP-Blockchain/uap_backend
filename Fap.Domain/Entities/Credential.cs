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
        [Required, MaxLength(120)] public required string CredentialId { get; set; } // On-chain ID / certificate number
        [MaxLength(200)] public required string IPFSHash { get; set; }
        [MaxLength(500)] public required string FileUrl { get; set; }
        [Required] public DateTime IssuedDate { get; set; }
        public bool IsRevoked { get; set; }

        [Required] public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))] public Student? Student { get; set; }

        [Required] public Guid CertificateTemplateId { get; set; }
        [ForeignKey(nameof(CertificateTemplateId))] public CertificateTemplate? CertificateTemplate { get; set; }

        // Blockchain integration
        [MaxLength(200)]
        public string? BlockchainTransactionHash { get; set; }
        public DateTime? BlockchainStoredAt { get; set; }
        public bool IsOnBlockchain { get; set; } = false;

        /// <summary>
        /// Credential ID on the blockchain smart contract (uint256)
        /// Maps to credentialCount in CredentialManagement.sol
        /// </summary>
        public long? BlockchainCredentialId { get; set; }

        // Certificate type and references
        [Required, MaxLength(50)]
        public string CertificateType { get; set; } = "SubjectCompletion"; // "SubjectCompletion", "SemesterCompletion", "RoadmapCompletion"

        // Reference to what this certificate is for
        public Guid? SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }

        public Guid? SemesterId { get; set; }
        [ForeignKey(nameof(SemesterId))]
        public Semester? Semester { get; set; }

        public Guid? StudentRoadmapId { get; set; }
        [ForeignKey(nameof(StudentRoadmapId))]
        public StudentRoadmap? StudentRoadmap { get; set; }

        // Certificate details
        public DateTime? CompletionDate { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? FinalGrade { get; set; }

        [MaxLength(10)]
        public string? LetterGrade { get; set; }

        [MaxLength(50)]
        public string? Classification { get; set; } // "Excellent", "Good", "Pass"

        // Verification and sharing
        [MaxLength(500)]
        public string? VerificationHash { get; set; } // SHA-256 hash for verification

        public string? QRCodeData { get; set; } // Base64 QR code image
        
        [MaxLength(500)]
        public string? ShareableUrl { get; set; } // Public URL for sharing

        // Status and review (admin workflow)
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Issued"; // "Pending", "Approved", "Issued", "Revoked"

        public Guid? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? ReviewNotes { get; set; }

        // Revocation
        public Guid? RevokedBy { get; set; }
        public DateTime? RevokedAt { get; set; }

        [MaxLength(500)]
        public string? RevocationReason { get; set; }

        // Sharing statistics
        public int ViewCount { get; set; } = 0;
        public DateTime? LastViewedAt { get; set; }

        // PDF generation
        [MaxLength(500)]
        public string? PdfFilePath { get; set; }

        [MaxLength(500)]
        public string? PdfUrl { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Certificate Request - Students request certificates, Admin approves
    /// </summary>
    [Table("CredentialRequests")]
    public class CredentialRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public Student Student { get; set; } = null!;

        [Required, MaxLength(50)]
        public string CertificateType { get; set; } = null!;

        // Reference to what the certificate is for
        public Guid? SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }

        public Guid? SemesterId { get; set; }
        [ForeignKey(nameof(SemesterId))]
        public Semester? Semester { get; set; }

        public Guid? StudentRoadmapId { get; set; }
        [ForeignKey(nameof(StudentRoadmapId))]
        public StudentRoadmap? StudentRoadmap { get; set; }

        // Request details
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        public DateTime? CompletionDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? FinalGrade { get; set; }

        [MaxLength(10)]
        public string? LetterGrade { get; set; }

        [MaxLength(50)]
        public string? Classification { get; set; }

        public bool IsAutoGenerated { get; set; } = true; // Auto-created when student completes

        [MaxLength(500)]
        public string? StudentNotes { get; set; }

        // Admin response
        public Guid? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNotes { get; set; }

        // Link to created credential if approved
        public Guid? CredentialId { get; set; }
        [ForeignKey(nameof(CredentialId))]
        public Credential? Credential { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
