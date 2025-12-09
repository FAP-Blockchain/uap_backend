using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IValidationService
    {
        Task<bool> IsAttendanceDateValidationEnabledAsync();
        Task SetAttendanceDateValidationAsync(bool enabled);
    }
}
