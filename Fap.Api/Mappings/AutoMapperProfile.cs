using AutoMapper;
using Fap.Domain.DTOs.Auth;
using Fap.Domain.DTOs.User;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ========== AUTH MAPPINGS ==========
            
            CreateMap<RegisterUserRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
                .ForMember(dest => dest.ActionLogs, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.Teacher, opt => opt.Ignore());

            CreateMap<RegisterUserRequest, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => 
                    src.EnrollmentDate ?? DateTime.UtcNow))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src => 0m))
                .ForMember(dest => dest.IsGraduated, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.GraduationDate, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Grades, opt => opt.Ignore())
                .ForMember(dest => dest.Enrolls, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore())
                .ForMember(dest => dest.ClassMembers, opt => opt.Ignore())
                .ForMember(dest => dest.Credentials, opt => opt.Ignore())
                .ForMember(dest => dest.Roadmaps, opt => opt.Ignore());

            CreateMap<RegisterUserRequest, Teacher>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.TeacherCode))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => 
                    src.HireDate ?? DateTime.UtcNow))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore());

            // ========== OTP MAPPINGS ==========
            
            CreateMap<SendOtpRequest, Otp>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => src.Purpose))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.UsedAt, opt => opt.Ignore());

            CreateMap<Otp, OtpResponse>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => src.Purpose))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => src.IsUsed));

            // ========== USER MAPPINGS ========== 
            
            // User -> UserResponse
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => 
                    src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => 
                    src.Teacher != null ? src.Teacher.TeacherCode : null));
        }
    }
}
