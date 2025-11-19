using AutoMapper;
using Fap.Domain.DTOs.StudentRoadmap;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    public class StudentRoadmapMappingProfile : Profile
    {
        public StudentRoadmapMappingProfile()
        {
            // Entity -> DTO
            CreateMap<StudentRoadmap, StudentRoadmapDto>()
                .ForMember(dest => dest.SubjectCode,
                    opt => opt.MapFrom(src => src.Subject.SubjectCode))
                .ForMember(dest => dest.SubjectName,
                    opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.Credits,
                    opt => opt.MapFrom(src => src.Subject.Credits))
                .ForMember(dest => dest.SemesterName,
                    opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(dest => dest.SemesterCode,
                    opt => opt.MapFrom(src => src.Semester.Name));

            CreateMap<StudentRoadmap, StudentRoadmapDetailDto>()
                .ForMember(dest => dest.StudentCode,
                    opt => opt.MapFrom(src => src.Student.StudentCode))
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentEmail,
                    opt => opt.MapFrom(src => src.Student.User.Email))
                .ForMember(dest => dest.SubjectCode,
                    opt => opt.MapFrom(src => src.Subject.SubjectCode))
                .ForMember(dest => dest.SubjectName,
                    opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.Credits,
                    opt => opt.MapFrom(src => src.Subject.Credits))
                .ForMember(dest => dest.SubjectDescription,
                    opt => opt.MapFrom(src => src.Subject.Description))
                .ForMember(dest => dest.SemesterName,
                    opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(dest => dest.SemesterCode,
                    opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(dest => dest.SemesterStartDate,
                    opt => opt.MapFrom(src => src.Semester.StartDate))
                .ForMember(dest => dest.SemesterEndDate,
                    opt => opt.MapFrom(src => src.Semester.EndDate));

            // Request -> Entity
            CreateMap<CreateStudentRoadmapRequest, StudentRoadmap>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.Subject, opt => opt.Ignore())
                .ForMember(dest => dest.Semester, opt => opt.Ignore())
                .ForMember(dest => dest.FinalScore, opt => opt.Ignore())
                .ForMember(dest => dest.LetterGrade, opt => opt.Ignore())
                .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
