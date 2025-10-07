using Fap.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;

namespace Fap.Domain.Entities
{
    [Table("Users")]
    public class User
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(150)] public string FullName { get; set; }
        [Required, MaxLength(150)] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        [MaxLength(30)] public string StudentCode { get; set; } // nếu là SV
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required] public Guid RoleId { get; set; }
        [ForeignKey(nameof(RoleId))] public Role Role { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
        public virtual ICollection<Credential> Credentials { get; set; }
        public virtual ICollection<ActionLog> ActionLogs { get; set; }
        public virtual ICollection<ClassMember> ClassMembers { get; set; }
        public virtual ICollection<Enroll> Enrolls { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<Grade> Grades { get; set; }
        public virtual ICollection<StudentTranscript> Transcripts { get; set; }
        public virtual ICollection<Class> CreatedClasses { get; set; } // teacher creates
        public virtual ICollection<Class> TaughtClasses { get; set; }  // teacher teaches
    }
}
