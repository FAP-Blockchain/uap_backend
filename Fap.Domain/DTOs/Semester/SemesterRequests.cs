using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Semester
{
    public class GetSemestersRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsClosed { get; set; }
        public string SortBy { get; set; } = "StartDate"; // Name, StartDate, EndDate
        public bool IsDescending { get; set; } = true;
    }

    public class CreateSemesterRequest
    {
        [Required(ErrorMessage = "Semester name is required")]
        [MaxLength(80, ErrorMessage = "Semester name cannot exceed 80 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        // ✅ Optional: Default settings for auto-created SubjectOfferings
        public int DefaultMaxClassesPerSubject { get; set; } = 10;
        public int DefaultSemesterCapacityPerSubject { get; set; } = 400;
        public int RegistrationStartDaysBeforeSemester { get; set; } = 14; // 2 weeks before
        public int RegistrationEndDaysAfterSemesterStart { get; set; } = 7; // 1 week after
    }

    public class UpdateSemesterRequest
    {
        [Required(ErrorMessage = "Semester name is required")]
        [MaxLength(80, ErrorMessage = "Semester name cannot exceed 80 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
    }

    public class UpdateSemesterActiveStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
