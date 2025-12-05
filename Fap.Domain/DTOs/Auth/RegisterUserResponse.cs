namespace Fap.Domain.DTOs.Auth
{
    public class RegisterUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? UserId { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string? WalletAddress { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public BlockchainInfo? Blockchain { get; set; }
    }
}