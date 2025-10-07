using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("GradeComponents")]
    public class GradeComponent
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(80)] public string Name { get; set; } // Quiz/Midterm/Final
        [Range(0, 100)] public int WeightPercent { get; set; }      // 0..100

        public virtual ICollection<Grade> Grades { get; set; }
    }
}
