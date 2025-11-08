namespace Fap.Domain.DTOs.Semester
{
    public class SemesterDto
    {
  public Guid Id { get; set; }
        public string Name { get; set; }
      public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
      public int TotalSubjects { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
    }

    public class SemesterDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
  public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudentsEnrolled { get; set; }
 public List<SubjectSummaryDto> Subjects { get; set; }
    }

    public class SubjectSummaryDto
    {
        public Guid Id { get; set; }
        public string SubjectCode { get; set; }
    public string SubjectName { get; set; }
        public int Credits { get; set; }
     public int TotalClasses { get; set; }
    }
}
