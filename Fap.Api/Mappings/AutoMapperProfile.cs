using AutoMapper;
using Fap.Domain.DTOs.Auth;
using Fap.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fap.Api.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // RegisterRequest -> User
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            // User -> UserDto
            
        }
    }
}
