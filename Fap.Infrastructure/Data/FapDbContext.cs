using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data
{
    public class FapDbContext : DbContext
    {
        public FapDbContext(DbContextOptions<FapDbContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassMember> ClassMembers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SubjectOffering> SubjectOfferings { get; set; }  // ✅ NEW
        public DbSet<SubjectCriteria> SubjectCriteria { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<GradeComponent> GradeComponents { get; set; }
        public DbSet<StudentRoadmap> StudentRoadmaps { get; set; }
        public DbSet<Credential> Credentials { get; set; }
        public DbSet<CredentialRequest> CredentialRequests { get; set; } // ✅ NEW
        public DbSet<CertificateTemplate> CertificateTemplates { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Slot> Slots { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Enroll> Enrolls { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Otp> Otps { get; set; }  // ✅ NEW
        public DbSet<Wallet> Wallets { get; set; }  // ✅ NEW - Blockchain Wallets
        public DbSet<Curriculum> Curriculums { get; set; }
        public DbSet<CurriculumSubject> CurriculumSubjects { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<TeacherSpecialization> TeacherSpecializations { get; set; }
    public DbSet<SubjectSpecialization> SubjectSpecializations { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== DECIMAL PRECISION ====================
            // Grade.Score
            modelBuilder.Entity<Grade>()
                .Property(g => g.Score)
                .HasPrecision(4, 2); // 0.00 - 99.99

            // Student.GPA
            modelBuilder.Entity<Student>()
                .Property(s => s.GPA)
                .HasPrecision(4, 2); // 0.00 - 99.99

            // SubjectCriteria.MinScore
            modelBuilder.Entity<SubjectCriteria>()
                .Property(sc => sc.MinScore)
                .HasPrecision(4, 2); // 0.00 - 99.99

            // ==================== RELATIONSHIPS ====================

            // User <-> Student/Teacher (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Curriculum)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.CurriculumId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherSpecialization>()
                .HasKey(ts => new { ts.TeacherId, ts.SpecializationId });

            modelBuilder.Entity<TeacherSpecialization>()
                .HasOne(ts => ts.Teacher)
                .WithMany(t => t.TeacherSpecializations)
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherSpecialization>()
                .HasOne(ts => ts.Specialization)
                .WithMany(s => s.TeacherSpecializations)
                .HasForeignKey(ts => ts.SpecializationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubjectSpecialization>()
                .HasKey(ss => new { ss.SubjectId, ss.SpecializationId });

            modelBuilder.Entity<SubjectSpecialization>()
                .HasOne(ss => ss.Subject)
                .WithMany(s => s.SubjectSpecializations)
                .HasForeignKey(ss => ss.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubjectSpecialization>()
                .HasOne(ss => ss.Specialization)
                .WithMany(s => s.SubjectSpecializations)
                .HasForeignKey(ss => ss.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role <-> Permission
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithOne(p => p.Role)
                .HasForeignKey(p => p.RoleId);

            // User <-> Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            // ClassMember composite key
            modelBuilder.Entity<ClassMember>()
                .HasKey(cm => new { cm.ClassId, cm.StudentId });

            // ClassMember relationships (Restrict to prevent cascade path errors)
            modelBuilder.Entity<ClassMember>()
                .HasOne(cm => cm.Class)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassMember>()
                .HasOne(cm => cm.Student)
                .WithMany(s => s.ClassMembers)
                .HasForeignKey(cm => cm.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ NEW: GradeComponent <-> Subject
            modelBuilder.Entity<GradeComponent>()
                .HasOne(gc => gc.Subject)
                .WithMany()
                .HasForeignKey(gc => gc.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // ✅ NEW: GradeComponent Self-Referencing (Parent-Child)
            modelBuilder.Entity<GradeComponent>()
                .HasOne(gc => gc.Parent)
                .WithMany(gc => gc.SubComponents)
                .HasForeignKey(gc => gc.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting parent if children exist

            // ✅ NEW: SubjectOffering <-> Subject
            modelBuilder.Entity<SubjectOffering>()
                .HasOne(so => so.Subject)
                .WithMany(s => s.Offerings)
                .HasForeignKey(so => so.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ NEW: SubjectOffering <-> Semester
            modelBuilder.Entity<SubjectOffering>()
                .HasOne(so => so.Semester)
                .WithMany(sem => sem.SubjectOfferings)
                .HasForeignKey(so => so.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ NEW: SubjectOffering <-> Class
            modelBuilder.Entity<Class>()
                .HasOne(c => c.SubjectOffering)
                .WithMany(so => so.Classes)
                .HasForeignKey(c => c.SubjectOfferingId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ NEW: Unique constraint on SubjectOffering (Subject + Semester)
            modelBuilder.Entity<SubjectOffering>()
                .HasIndex(so => new { so.SubjectId, so.SemesterId })
                .IsUnique()
                .HasDatabaseName("UK_SubjectOffering_Subject_Semester");

            // Subject <-> SubjectCriteria
            modelBuilder.Entity<Subject>()
                .HasMany(s => s.SubjectCriterias)
                .WithOne(sc => sc.Subject)
                .HasForeignKey(sc => sc.SubjectId);

            // Slot <-> Attendance (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Slot>()
                .HasMany(s => s.Attendances)
                .WithOne(a => a.Slot)
                .HasForeignKey(a => a.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subject <-> Grade
            modelBuilder.Entity<Subject>()
                .HasMany(s => s.Grades)
                .WithOne(g => g.Subject)
                .HasForeignKey(g => g.SubjectId);

            // Student <-> Grade
            modelBuilder.Entity<Student>()
                .HasMany(s => s.Grades)
                .WithOne(g => g.Student)
                .HasForeignKey(g => g.StudentId);

            // Teacher <-> Class
            modelBuilder.Entity<Teacher>()
                .HasMany(t => t.Classes)
                .WithOne(c => c.Teacher)
                .HasForeignKey(c => c.TeacherUserId);

            // Student <-> Enroll (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Student>()
                .HasMany(s => s.Enrolls)
                .WithOne(e => e.Student)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Class <-> Enroll
            modelBuilder.Entity<Class>()
                .HasMany(c => c.Enrolls)
                .WithOne(e => e.Class)
                .HasForeignKey(e => e.ClassId);

            // User <-> RefreshToken
            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId);

            // TimeSlot <-> Slot
            modelBuilder.Entity<Slot>()
                .HasOne(s => s.TimeSlot)
                .WithMany(ts => ts.Slots)
                .HasForeignKey(s => s.TimeSlotId);

            // Class <-> Slot
            modelBuilder.Entity<Slot>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Slots)
                .HasForeignKey(s => s.ClassId);

            // Slot <-> SubstituteTeacher (Restrict to prevent cascade)
            modelBuilder.Entity<Slot>()
                .HasOne(s => s.SubstituteTeacher)
                .WithMany()
                .HasForeignKey(s => s.SubstituteTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Credential <-> Student
            modelBuilder.Entity<Credential>()
                .HasOne(c => c.Student)
                .WithMany(s => s.Credentials)
                .HasForeignKey(c => c.StudentId);

            // Credential <-> CertificateTemplate
            modelBuilder.Entity<Credential>()
                .HasOne(c => c.CertificateTemplate)
                .WithMany(ct => ct.Credentials)
                .HasForeignKey(c => c.CertificateTemplateId);

            // ActionLog <-> User
            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.ActionLogs)
                .HasForeignKey(a => a.UserId);

            // Attendance <-> Student (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Attendance <-> Slot (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Slot)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enroll <-> Class (Restrict to prevent cascade path errors)
            modelBuilder.Entity<Enroll>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrolls)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== STUDENT ROADMAP ====================
            // StudentRoadmap <-> Student (🔴 RESTRICT to prevent cascade cycle)
            modelBuilder.Entity<StudentRoadmap>()
                .HasOne(sr => sr.Student)
                .WithMany(s => s.Roadmaps)
                .HasForeignKey(sr => sr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentRoadmap <-> Subject (🔴 RESTRICT to prevent cascade cycle)
            modelBuilder.Entity<StudentRoadmap>()
                .HasOne(sr => sr.Subject)
                .WithMany(s => s.Roadmaps)
                .HasForeignKey(sr => sr.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentRoadmap <-> Semester (🔴 RESTRICT to prevent cascade cycle)
            modelBuilder.Entity<StudentRoadmap>()
                .HasOne(sr => sr.Semester)
                .WithMany()
                .HasForeignKey(sr => sr.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== CURRICULUM ====================
            modelBuilder.Entity<CurriculumSubject>()
                .HasOne(cs => cs.Curriculum)
                .WithMany(c => c.CurriculumSubjects)
                .HasForeignKey(cs => cs.CurriculumId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CurriculumSubject>()
                .HasOne(cs => cs.Subject)
                .WithMany(s => s.CurriculumSubjects)
                .HasForeignKey(cs => cs.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurriculumSubject>()
                .HasOne(cs => cs.PrerequisiteSubject)
                .WithMany()
                .HasForeignKey(cs => cs.PrerequisiteSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurriculumSubject>()
                .HasIndex(cs => new { cs.CurriculumId, cs.SubjectId })
                .IsUnique()
                .HasDatabaseName("UK_Curriculum_Subject");

            // ✅ OTP Configuration
            modelBuilder.Entity<Otp>()
                .HasIndex(o => new { o.Email, o.Code, o.Purpose })
                .HasDatabaseName("IX_Otp_Email_Code_Purpose");

            modelBuilder.Entity<Otp>()
                .HasIndex(o => o.ExpiresAt)
                .HasDatabaseName("IX_Otp_ExpiresAt");

            // ✅ WALLET Configuration
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.Address)
                .IsUnique()
                .HasDatabaseName("IX_Wallet_Address");

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .HasDatabaseName("IX_Wallet_UserId");

            // Wallet <-> User (Optional relationship - wallet can exist without user)
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}