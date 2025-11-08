namespace Fap.Domain.DTOs.Subject
{
    public class SubjectDto
    {
        public Guid Id { get; set; }
   public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
  public int Credits { get; set; }
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
      public int TotalClasses { get; set; }
    }

    public class SubjectDetailDto
    {
        public Guid Id { get; set; }
      public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int Credits { get; set; }
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; }
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public List<ClassSummaryDto> Classes { get; set; }
        public int TotalStudentsEnrolled { get; set; }
  }

    public class ClassSummaryDto
    {
        public Guid Id { get; set; }
        public string ClassCode { get; set; }
        public string TeacherName { get; set; }
      public int CurrentEnrollment { get; set; }
  public int MaxEnrollment { get; set; }
    }
}
