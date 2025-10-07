using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Subjects")]
    public class Subject
    {
        [Key] public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string SubjectCode { get; set; }

        [Required, MaxLength(150)]
        public string SubjectName { get; set; }

        public int Credits { get; set; }

        [Required]
        public Guid SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public Semester Semester { get; set; }

        // ⬇️ Thêm các navigation cần cho DbContext
        public virtual ICollection<Class> Classes { get; set; }
        public virtual ICollection<Slot> Slots { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<Grade> Grades { get; set; }
        public virtual ICollection<StudentTranscript> Transcripts { get; set; }
    }
}
