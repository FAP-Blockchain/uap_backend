using AutoMapper;
using Fap.Domain.DTOs.Specialization;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    public class SpecializationMappingProfile : Profile
    {
        public SpecializationMappingProfile()
        {
            CreateMap<Specialization, SpecializationDto>();
            CreateMap<Specialization, SpecializationDetailDto>();
            CreateMap<CreateSpecializationRequest, Specialization>();
            CreateMap<UpdateSpecializationRequest, Specialization>();
        }
    }
}
