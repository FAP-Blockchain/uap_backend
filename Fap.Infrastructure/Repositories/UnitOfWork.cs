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
        public IClassMemberRepository ClassMembers { get; }
        public ISubjectRepository Subjects { get; }
        public ISubjectOfferingRepository SubjectOfferings { get; }
        public ITimeSlotRepository TimeSlots { get; }
        public ISemesterRepository Semesters { get; }
        public IEnrollRepository Enrolls { get; }
        public IGradeRepository Grades { get; }
        public IGradeComponentRepository GradeComponents { get; }
        public IAttendanceRepository Attendances { get; }
        public ISlotRepository Slots { get; }
        public IStudentRoadmapRepository StudentRoadmaps { get; }
        public IWalletRepository Wallets { get; }
        public ICurriculumRepository Curriculums { get; }
        public ICurriculumSubjectRepository CurriculumSubjects { get; }
        public ICredentialRepository Credentials { get; }
        public ICredentialRequestRepository CredentialRequests { get; }
        public ICertificateTemplateRepository CertificateTemplates { get; }
        public ISubjectCriteriaRepository SubjectCriteria { get; }
    public ISpecializationRepository Specializations { get; }
        
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
            ClassMembers = new ClassMemberRepository(context); 
            Subjects = new SubjectRepository(context);
            SubjectOfferings = new SubjectOfferingRepository(context); 
            TimeSlots = new TimeSlotRepository(context);
            Semesters = new SemesterRepository(context);
            Enrolls = new EnrollRepository(context);
            Grades = new GradeRepository(context);
            GradeComponents = new GradeComponentRepository(context);
            Attendances = new AttendanceRepository(context);
            Slots = new SlotRepository(context);
            StudentRoadmaps = new StudentRoadmapRepository(context);
            Wallets = new WalletRepository(context);
            Curriculums = new CurriculumRepository(context);
            CurriculumSubjects = new CurriculumSubjectRepository(context);
            Credentials = new CredentialRepository(context);
            CredentialRequests = new CredentialRequestRepository(context);
            CertificateTemplates = new CertificateTemplateRepository(context);
            SubjectCriteria = new SubjectCriteriaRepository(context);
            Specializations = new SpecializationRepository(context);
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
