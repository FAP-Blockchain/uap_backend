using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Fap.Api.DTOs.Student
{
    public class StudentProfileImageUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = default!;
    }
}
