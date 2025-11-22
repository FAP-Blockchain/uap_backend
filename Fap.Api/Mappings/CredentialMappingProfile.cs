using AutoMapper;
using Fap.Domain.DTOs.Credential;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
 public class CredentialMappingProfile : Profile
    {
        public CredentialMappingProfile()
      {
      // Credential mappings
      CreateMap<Credential, CredentialDto>()
 .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
    .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
   .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
    .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null)) // ? FIXED: Use Name instead of SemesterName
.ForMember(dest => dest.RoadmapName, opt => opt.MapFrom(src => src.StudentRoadmap != null ? "Student Roadmap" : null));

      CreateMap<Credential, CertificatePublicDto>()
        .ForMember(dest => dest.CredentialNumber, opt => opt.MapFrom(src => src.CredentialId))
        .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
        .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
        .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
        .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null))
        .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => src.CertificateTemplate != null ? src.CertificateTemplate.Name : null));

      CreateMap<Credential, CredentialDetailDto>()
   .IncludeBase<Credential, CredentialDto>()
   .ForMember(dest => dest.Template, opt => opt.MapFrom(src => src.CertificateTemplate))
     .ForMember(dest => dest.ReviewedByName, opt => opt.MapFrom(src => src.ReviewedBy.HasValue ? "Admin" : null));

  // Credential Request mappings
  CreateMap<CredentialRequest, CredentialRequestDto>()
        .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
   .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
        .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
    .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null)) // ? FIXED
        .ForMember(dest => dest.RoadmapName, opt => opt.MapFrom(src => src.StudentRoadmap != null ? "Student Roadmap" : null));

         // Template mappings
       CreateMap<CertificateTemplate, CertificateTemplateDto>();

    // Reverse mappings for updates
       CreateMap<CreateCertificateTemplateRequest, CertificateTemplate>();
 CreateMap<UpdateCertificateTemplateRequest, CertificateTemplate>()
     .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
