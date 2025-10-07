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
        [Required] public DateTime RegistrationStart { get; set; }
        [Required] public DateTime RegistrationEnd { get; set; }

        public virtual ICollection<Subject> Subjects { get; set; }
    }
}
