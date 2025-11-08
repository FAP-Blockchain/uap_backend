using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Semesters")]
    public class Semester
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(80)] public string Name { get; set; } // e.g., Spring 2026
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        
        public bool IsClosed { get; set; } = false;  // ✅ NEW: Trạng thái đóng học kỳ

        public virtual ICollection<Subject> Subjects { get; set; }
    }
}
