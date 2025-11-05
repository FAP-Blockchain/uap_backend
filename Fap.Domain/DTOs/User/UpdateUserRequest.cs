using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.User
{
    public class UpdateUserRequest
    {
        [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string? Email { get; set; }

        public string? RoleName { get; set; } // Admin, Teacher, Student

        // Student fields
        [MaxLength(30)]
        public string? StudentCode { get; set; }
        public DateTime? EnrollmentDate { get; set; }

        // Teacher fields
        [MaxLength(30)]
        public string? TeacherCode { get; set; }
        public DateTime? HireDate { get; set; }
        [MaxLength(200)]
        public string? Specialization { get; set; }
        [MaxLength(20)]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }
    }
}