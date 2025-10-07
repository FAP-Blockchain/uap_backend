using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data;

public class FapDbContext : DbContext
{
    public FapDbContext(DbContextOptions<FapDbContext> options) : base(options) { }

    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<ClassMember> ClassMembers { get; set; }
    public DbSet<Enroll> Enrolls { get; set; }
    public DbSet<Slot> Slots { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<GradeComponent> GradeComponents { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<StudentTranscript> StudentTranscripts { get; set; }
    public DbSet<CertificateTemplate> CertificateTemplates { get; set; }
    public DbSet<Credential> Credentials { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== USER ==========
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();


        modelBuilder.Entity<Class>()
            .HasOne(c => c.Teacher)
            .WithMany()
            .HasForeignKey(c => c.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== SUBJECT / SEMESTER ==========
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.SubjectCode)
            .IsUnique();

        modelBuilder.Entity<Subject>()
            .HasOne(s => s.Semester)
            .WithMany(se => se.Subjects)
            .HasForeignKey(s => s.SemesterId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========== SLOT / TIMESLOT ==========
        // Slot thuộc Subject, có 1 TimeSlot (giờ học)
        modelBuilder.Entity<Slot>()
            .HasOne(s => s.Subject)
            .WithMany(sub => sub.Slots)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Slot>()
            .HasOne(s => s.TimeSlot)
            .WithMany(ts => ts.Slots)
            .HasForeignKey(s => s.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimeSlot có tên như Slot 1, Slot 2,...
        modelBuilder.Entity<TimeSlot>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // ========== CLASS ==========
        modelBuilder.Entity<Class>()
            .HasIndex(c => c.ClassCode)
            .IsUnique();

        modelBuilder.Entity<Class>()
            .HasOne(c => c.Teacher)
            .WithMany()
            .HasForeignKey(c => c.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);


        // ========== ATTENDANCE ==========
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.User)
            .WithMany(u => u.Attendances)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

       
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Subject)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Slot)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.SlotId)
            .OnDelete(DeleteBehavior.Cascade);


        // ========== GRADE ==========
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.User)
            .WithMany(u => u.Grades)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Subject)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.GradeComponent)
            .WithMany(gc => gc.Grades)
            .HasForeignKey(g => g.GradeComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== TRANSCRIPT ==========
        modelBuilder.Entity<StudentTranscript>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transcripts)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentTranscript>()
            .HasOne(t => t.Subject)
            .WithMany(s => s.Transcripts)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);


        // ========== CREDENTIAL ==========
        modelBuilder.Entity<Credential>()
            .HasIndex(c => c.CredentialId)
            .IsUnique();

        modelBuilder.Entity<Credential>()
            .HasOne(c => c.User)
            .WithMany(u => u.Credentials)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Credential>()
            .HasOne(c => c.CertificateTemplate)
            .WithMany(ct => ct.Credentials)
            .HasForeignKey(c => c.CertificateTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== ACTION LOG ==========
        modelBuilder.Entity<ActionLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.ActionLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActionLog>()
            .HasOne(al => al.Credential)
            .WithMany()
            .HasForeignKey(al => al.CredentialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
