using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Common
{
    public class PaginationRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc"; // "asc" or "desc"
    }
}