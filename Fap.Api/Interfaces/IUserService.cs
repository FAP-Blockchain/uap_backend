using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.User;

namespace Fap.Api.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserResponse>> GetUsersAsync(GetUsersRequest request);
        Task<UserResponse?> GetUserByIdAsync(Guid id);
        Task<UpdateUserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request); 
        Task<UpdateUserResponse> ActivateUserAsync(Guid id); 
        Task<UpdateUserResponse> DeactivateUserAsync(Guid id); 
        Task<string> UpdateProfileImageAsync(Guid userId, Stream imageStream, string fileName);
        Task<UpdateUserResponse> UpdateUserOnChainAsync(Guid userId, UpdateUserOnChainRequest request);
    }
}