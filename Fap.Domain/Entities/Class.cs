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
    [Table("Classes")]
    public class Class
    {
        [Key] public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string ClassCode { get; set; }

        [Required]
        public Guid SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; }

        // ✅ Giáo viên dạy lớp này
        [Required]
        public Guid TeacherUserId { get; set; }
        [ForeignKey(nameof(TeacherUserId))]
        public User Teacher { get; set; }

        // ✅ Sinh viên trong lớp
        public virtual ICollection<ClassMember> Members { get; set; }

        // ✅ Đăng ký lớp
        public virtual ICollection<Enroll> Enrolls { get; set; }
    }
}
