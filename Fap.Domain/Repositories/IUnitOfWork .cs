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
        ISubjectRepository Subjects { get; }
        ITimeSlotRepository TimeSlots { get; }
        ISemesterRepository Semesters { get; }
        IEnrollRepository Enrolls { get; }
        
        Task<int> SaveChangesAsync();
        void ClearChangeTracker();
    }
}
