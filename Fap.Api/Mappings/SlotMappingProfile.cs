using AutoMapper;
using Fap.Domain.DTOs.Slot;
using Fap.Domain.DTOs.Class;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    /// <summary>
    /// AutoMapper profile for Slot entities
    /// ✅ UPDATED: Uses SubjectOffering pattern via Class
    /// </summary>
    public class SlotMappingProfile : Profile
    {
        public SlotMappingProfile()
        {
            // ======================================================================
            // SLOT MAPPINGS
            // ======================================================================

            CreateMap<Slot, SlotDto>()
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src =>
                    src.Class != null ? src.Class.ClassCode : null))
                // ✅ CHANGED: Get subject via SubjectOffering
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src =>
                    src.Class != null && src.Class.SubjectOffering != null && src.Class.SubjectOffering.Subject != null
                        ? src.Class.SubjectOffering.Subject.SubjectName
                        : null))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src =>
                    src.Class != null && src.Class.Teacher != null && src.Class.Teacher.User != null
                        ? src.Class.Teacher.User.FullName
                        : null))
                .ForMember(dest => dest.TimeSlotName, opt => opt.MapFrom(src =>
                    src.TimeSlot != null ? src.TimeSlot.Name : null))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src =>
                    src.TimeSlot != null ? src.TimeSlot.StartTime : (TimeSpan?)null))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src =>
                    src.TimeSlot != null ? src.TimeSlot.EndTime : (TimeSpan?)null))
                .ForMember(dest => dest.SubstituteTeacherName, opt => opt.MapFrom(src =>
                    src.SubstituteTeacher != null && src.SubstituteTeacher.User != null
                        ? src.SubstituteTeacher.User.FullName
                        : null))
                .ForMember(dest => dest.HasAttendance, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAttendances, opt => opt.Ignore())
                .ForMember(dest => dest.PresentCount, opt => opt.Ignore())
                .ForMember(dest => dest.AbsentCount, opt => opt.Ignore());

            // ======================================================================
            // SLOT SUMMARY MAPPINGS (for ClassDetailDto)
            // ======================================================================

            CreateMap<Slot, SlotSummaryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.TimeSlotName, opt => opt.MapFrom(src =>
                    src.TimeSlot != null ? src.TimeSlot.Name : "TBA"))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
        }
    }
}
