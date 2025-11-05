namespace Fap.Domain.DTOs.User
{
    public class UpdateUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? UserId { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}