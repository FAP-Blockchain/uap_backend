using AutoMapper;
using Fap.Domain.DTOs.Class;
using Fap.Domain.DTOs.Subject;
using Fap.Domain.DTOs.Slot;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    /// <summary>
    /// AutoMapper profile for Class entities
    /// ? UPDATED: Uses SubjectOffering pattern
    /// </summary>
    public class ClassMappingProfile : Profile
    {
        public ClassMappingProfile()
        {
            // ======================================================================
            // CLASS MAPPINGS
            // ======================================================================

            CreateMap<Class, ClassDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))

                // ? CHANGED: Get subject info via SubjectOffering
                .ForMember(dest => dest.SubjectOfferingId, opt => opt.MapFrom(src => src.SubjectOfferingId))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null ? src.SubjectOffering.SubjectId : Guid.Empty))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.SubjectCode
                        : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.SubjectName
                        : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.Credits
                        : 0))

                // ? CHANGED: Get semester info via SubjectOffering
                .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null ? src.SubjectOffering.SemesterId : Guid.Empty))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Semester != null
                        ? src.SubjectOffering.Semester.Name
                        : null))

                // Teacher info
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherUserId))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null
                        ? src.Teacher.User.FullName
                        : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src =>
                    src.Teacher != null ? src.Teacher.TeacherCode : null))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null
                        ? src.Teacher.User.Email
                        : null))
                .ForMember(dest => dest.TeacherPhone, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null ? src.Teacher.User.PhoneNumber : null))  // ? FIXED: Use User.PhoneNumber

                // Class info
                .ForMember(dest => dest.MaxEnrollment, opt => opt.MapFrom(src => src.MaxEnrollment))
                .ForMember(dest => dest.CurrentEnrollment, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<Class, ClassDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))

                // ? CHANGED: Subject via SubjectOffering
                .ForMember(dest => dest.SubjectOfferingId, opt => opt.MapFrom(src => src.SubjectOfferingId))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null ? src.SubjectOffering.SubjectId : Guid.Empty))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.SubjectCode
                        : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.SubjectName
                        : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Subject != null
                        ? src.SubjectOffering.Subject.Credits
                        : 0))

                // ? CHANGED: Semester via SubjectOffering
                .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null ? src.SubjectOffering.SemesterId : Guid.Empty))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src =>
                    src.SubjectOffering != null && src.SubjectOffering.Semester != null
                        ? src.SubjectOffering.Semester.Name
                        : null))

                // Teacher info
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherUserId))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null
                        ? src.Teacher.User.FullName
                        : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src =>
                    src.Teacher != null ? src.Teacher.TeacherCode : null))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null
                        ? src.Teacher.User.Email
                        : null))

                // Class info
                .ForMember(dest => dest.MaxEnrollment, opt => opt.MapFrom(src => src.MaxEnrollment))
                .ForMember(dest => dest.CurrentEnrollment, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))

                // Collections
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.Members))
                .ForMember(dest => dest.Slots, opt => opt.MapFrom(src => src.Slots))
                .ForMember(dest => dest.SlotDetails, opt => opt.MapFrom(src => src.Slots));

            // ======================================================================
            // CLASS SUMMARY MAPPINGS
            // ======================================================================

            CreateMap<Class, ClassSummaryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src =>
                    src.Teacher != null && src.Teacher.User != null
                        ? src.Teacher.User.FullName
                        : null))
                .ForMember(dest => dest.CurrentEnrollment, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.MaxEnrollment, opt => opt.MapFrom(src => src.MaxEnrollment));

            // ======================================================================
            // CLASS MEMBER MAPPINGS
            // ======================================================================

            CreateMap<ClassMember, ClassStudentInfo>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.Student != null && src.Student.User != null
                        ? src.Student.User.FullName
                        : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    src.Student != null && src.Student.User != null
                        ? src.Student.User.Email
                        : null))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.GPA : 0m))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt));

            CreateMap<ClassMember, AssignedStudentInfo>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
                    src.Student != null && src.Student.User != null
                        ? src.Student.User.FullName
                        : null))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt));
        }
    }
}
