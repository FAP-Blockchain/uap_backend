using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    [Table("Slots")]
    public class Slot
    {
        [Key] public Guid Id { get; set; }

        [Required] public Guid SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))] public Subject Subject { get; set; }

        [Required] public DateTime Date { get; set; } // Ngày học cụ thể
        

        // Mỗi Slot có thể dùng 1 TimeSlot để chỉ thời gian bắt đầu–kết thúc
        public Guid? TimeSlotId { get; set; }
        [ForeignKey(nameof(TimeSlotId))] public TimeSlot TimeSlot { get; set; }

        // Attendance (điểm danh) theo Slot
        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}
