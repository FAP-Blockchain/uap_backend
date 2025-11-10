using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Auth
{
  public class ChangePasswordWithOtpRequest
    {
        [Required(ErrorMessage = "OTP code is required")]
        public string OtpCode { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }

   [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
     public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
