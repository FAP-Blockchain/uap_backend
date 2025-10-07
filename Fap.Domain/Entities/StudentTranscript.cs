using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("StudentTranscripts")]
    public class StudentTranscript
    {
        [Key] public Guid Id { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; }

        [Required] public Guid SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))] public Subject Subject { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        public decimal? FinalScore { get; set; }

        [MaxLength(5)] public string FinalLetter { get; set; }
        public bool Passed { get; set; }

        [MaxLength(100)] public string AcademicPhase { get; set; } // ví dụ: “Year 1 Semester 2”
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
