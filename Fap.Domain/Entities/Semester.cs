using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fap.Domain.Entities
{
    [Table("Semesters")]
    public class Semester
    {
        [Key] 
        public Guid Id { get; set; }
        
        [Required, MaxLength(80)] 
        public string Name { get; set; } // e.g., Spring 2024, Fall 2024
   
        [Required] 
        public DateTime StartDate { get; set; }
  
        [Required] 
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
    public bool IsClosed { get; set; } = false;  // Indicates the semester has ended

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Subjects offered during this semester
        public virtual ICollection<SubjectOffering> SubjectOfferings { get; set; } = new List<SubjectOffering>();
    }
}
