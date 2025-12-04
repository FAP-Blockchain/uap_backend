using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IValidationService
    {
        bool IsAttendanceDateValidationEnabled { get; }
        Task SetAttendanceDateValidationAsync(bool enabled);
    }
}
