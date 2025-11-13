using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;

namespace Fap.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FapDbContext _context;

        public IUserRepository Users { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IStudentRepository Students { get; }
        public ITeacherRepository Teachers { get; }
        public IRoleRepository Roles { get; }
        public IPermissionRepository Permissions { get; }
        public IOtpRepository Otps { get; }
        public IClassRepository Classes { get; }
        public ISubjectRepository Subjects { get; }
        public ITimeSlotRepository TimeSlots { get; }
        public ISemesterRepository Semesters { get; }
        public IEnrollRepository Enrolls { get; }
        public IGradeRepository Grades { get; }
        public IGradeComponentRepository GradeComponents { get; }

        public UnitOfWork(FapDbContext context)
        {
            _context = context;
            Users = new UserRepository(context);
            RefreshTokens = new RefreshTokenRepository(context);
            Students = new StudentRepository(context);
            Teachers = new TeacherRepository(context);
            Roles = new RoleRepository(context);
            Permissions = new PermissionRepository(context);
            Otps = new OtpRepository(context);
            Classes = new ClassRepository(context);
            Subjects = new SubjectRepository(context);
            TimeSlots = new TimeSlotRepository(context);
            Semesters = new SemesterRepository(context);
            Enrolls = new EnrollRepository(context);
            Grades = new GradeRepository(context);
            GradeComponents = new GradeComponentRepository(context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
