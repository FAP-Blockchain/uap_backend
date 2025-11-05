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
        
        // Optional: Student/Teacher info
        public string? StudentCode { get; set; }
        public string? TeacherCode { get; set; }
    }
}