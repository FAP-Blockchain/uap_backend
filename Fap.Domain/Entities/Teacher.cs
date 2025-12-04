using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fap.Domain.Entities
{
    public class Teacher
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }  // Liên kết với User

        [Required, MaxLength(30)]
        public string TeacherCode { get; set; } // Mã giáo viên

        public DateTime HireDate { get; set; } // Ngày bắt đầu làm việc

        [MaxLength(200)]
        public string? Specialization { get; set; } // Legacy single specialization reference

        public User User { get; set; }  // Quan hệ với User
        public virtual ICollection<Class> Classes { get; set; }
        public virtual ICollection<TeacherSpecialization> TeacherSpecializations { get; set; } = new List<TeacherSpecialization>();
    }
}
