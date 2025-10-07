using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("TimeSlots")]
    public class TimeSlot
    {
        [Key] public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } // e.g., Slot 1, Slot 2
        [Required] public TimeSpan StartTime { get; set; }
        [Required] public TimeSpan EndTime { get; set; }

        public virtual ICollection<Slot> Slots { get; set; }
    }
}
