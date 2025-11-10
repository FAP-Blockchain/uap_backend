using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Auth
{
    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        // Purpose is optional - just for logging/tracking, not validation
        public string? Purpose { get; set; }
    }
}