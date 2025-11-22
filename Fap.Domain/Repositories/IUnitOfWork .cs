using System;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IStudentRepository Students { get; }
        ITeacherRepository Teachers { get; }
        IRoleRepository Roles { get; }
        IPermissionRepository Permissions { get; }
        IOtpRepository Otps { get; }
        IClassRepository Classes { get; }
        IClassMemberRepository ClassMembers { get; } 
        ISubjectRepository Subjects { get; }
        ISubjectOfferingRepository SubjectOfferings { get; } 
        ITimeSlotRepository TimeSlots { get; }
        ISemesterRepository Semesters { get; }
        IEnrollRepository Enrolls { get; }
        IGradeRepository Grades { get; }
        IGradeComponentRepository GradeComponents { get; }
        IAttendanceRepository Attendances { get; }
        ISlotRepository Slots { get; }
        IWalletRepository Wallets { get; }
        IStudentRoadmapRepository StudentRoadmaps { get; }
        ICurriculumRepository Curriculums { get; }
        ICurriculumSubjectRepository CurriculumSubjects { get; }
        
        // ✅ NEW: Credential repositories
        ICredentialRepository Credentials { get; }
        ICredentialRequestRepository CredentialRequests { get; }
        ICertificateTemplateRepository CertificateTemplates { get; }
        ISubjectCriteriaRepository SubjectCriteria { get; }
        
        Task<int> SaveChangesAsync();
        void ClearChangeTracker();
    }
}
