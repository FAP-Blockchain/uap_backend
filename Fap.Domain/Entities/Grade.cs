using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Grades")]
    public class Grade
    {
        [Key] public Guid Id { get; set; }

        [Required] public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))] public User User { get; set; }

        [Required] public Guid SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))] public Subject Subject { get; set; }

        [Required] public Guid GradeComponentId { get; set; }
        [ForeignKey(nameof(GradeComponentId))] public GradeComponent GradeComponent { get; set; }

        
        [Range(0, 10)] public decimal Score { get; set; }
        [MaxLength(5)] public string LetterGrade { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
