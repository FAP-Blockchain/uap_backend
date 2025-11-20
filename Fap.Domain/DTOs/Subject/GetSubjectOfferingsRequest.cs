namespace Fap.Domain.DTOs.Subject
{
    public class GetSubjectOfferingsRequest
    {
public Guid? SemesterId { get; set; }
        public Guid? SubjectId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
      public string? SearchTerm { get; set; }
  }
}
