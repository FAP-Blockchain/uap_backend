namespace Fap.Domain.DTOs.User
{
    /// <summary>
    /// Request from frontend to persist on-chain registration info for a user.
    /// </summary>
    public class UpdateUserOnChainRequest
    {
        /// <summary>
        /// Blockchain transaction hash of the user registration.
        /// </summary>
        public string TransactionHash { get; set; } = string.Empty;

        /// <summary>
        /// Block number where the user registration transaction was included.
        /// NOTE: This value is ignored by backend; it will be derived from the on-chain receipt.
        /// </summary>
        public long BlockNumber { get; set; }

        /// <summary>
        /// Optional: on-chain registration timestamp (UTC).
        /// NOTE: This value is ignored by backend; it will be derived from the on-chain block timestamp when possible.
        /// </summary>
        public DateTime? RegisteredAtUtc { get; set; }
    }
}
