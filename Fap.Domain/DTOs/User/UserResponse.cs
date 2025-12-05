using System;
using System.Collections.Generic;

namespace Fap.Domain.DTOs.User
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RoleName { get; set; }
        
        // ? NEW: Contact Info
        public string? PhoneNumber { get; set; }

    // Profile image (stored at User level)
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImagePublicId { get; set; }
        
        // Optional: Student/Teacher info
        public string? StudentCode { get; set; }
        public string? TeacherCode { get; set; }
    public List<Guid> SpecializationIds { get; set; } = new();
    public List<Fap.Domain.DTOs.Specialization.SpecializationDto> Specializations { get; set; } = new();
        
        // ======================================================================
        // BLOCKCHAIN INTEGRATION
        // ======================================================================
        
        /// <summary>
        /// User's blockchain wallet address
        /// </summary>
        public string? WalletAddress { get; set; }
        
        /// <summary>
        /// Blockchain registration transaction hash
        /// </summary>
        public string? BlockchainTxHash { get; set; }
        
        /// <summary>
        /// Block number where user was registered
        /// </summary>
        public long? BlockNumber { get; set; }
        
        /// <summary>
        /// Blockchain registration timestamp
        /// </summary>
        public DateTime? BlockchainRegisteredAt { get; set; }
        
        /// <summary>
        /// Indicates if user is registered on blockchain
        /// </summary>
        public bool IsOnBlockchain => !string.IsNullOrEmpty(WalletAddress) 
                                    && !string.IsNullOrEmpty(BlockchainTxHash);
    }
}