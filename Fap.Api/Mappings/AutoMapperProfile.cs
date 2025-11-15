using AutoMapper;
using Fap.Domain.DTOs.Auth;
using Fap.Domain.DTOs.User;
using Fap.Domain.DTOs.Student;
using Fap.Domain.DTOs.Teacher;
using Fap.Domain.DTOs.Class;
using Fap.Domain.DTOs.TimeSlot;
using Fap.Domain.DTOs.Subject;
using Fap.Domain.DTOs.Semester;
using Fap.Domain.DTOs.Enrollment;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs.Slot;
using Fap.Domain.DTOs.Grade;
using Fap.Domain.DTOs.GradeComponent;
using Fap.Domain.Entities;

namespace Fap.Api.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ======================================================================
            // AUTH MAPPINGS
            // ======================================================================

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
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate ?? DateTime.UtcNow))
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
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate ?? DateTime.UtcNow))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore());

            // ======================================================================
            // OTP MAPPINGS
            // ======================================================================

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

            // ======================================================================
            // USER MAPPINGS
            // ======================================================================

            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.TeacherCode : null));

            // ======================================================================
            // STUDENT MAPPINGS
            // ======================================================================

            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src => src.GPA))
                .ForMember(dest => dest.IsGraduated, opt => opt.MapFrom(src => src.IsGraduated))
                .ForMember(dest => dest.GraduationDate, opt => opt.MapFrom(src => src.GraduationDate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.ClassMembers != null ? src.ClassMembers.Count : 0));

            CreateMap<Student, StudentDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src => src.GPA))
                .ForMember(dest => dest.IsGraduated, opt => opt.MapFrom(src => src.IsGraduated))
                .ForMember(dest => dest.GraduationDate, opt => opt.MapFrom(src => src.GraduationDate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.User.CreatedAt))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.ApprovedEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count(e => e.IsApproved) : 0))
                .ForMember(dest => dest.PendingEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count(e => !e.IsApproved) : 0))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.ClassMembers != null ? src.ClassMembers.Count : 0))
                .ForMember(dest => dest.TotalGrades, opt => opt.MapFrom(src => src.Grades != null ? src.Grades.Count : 0))
                .ForMember(dest => dest.TotalAttendances, opt => opt.MapFrom(src => src.Attendances != null ? src.Attendances.Count : 0))
                .ForMember(dest => dest.Enrollments, opt => opt.MapFrom(src => src.Enrolls))
                .ForMember(dest => dest.CurrentClasses, opt => opt.MapFrom(src => src.ClassMembers));

            // ======================================================================
            // TEACHER MAPPINGS
            // ======================================================================

            CreateMap<Teacher, TeacherDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.TeacherCode))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Classes != null ? src.Classes.Count : 0));

            CreateMap<Teacher, TeacherDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.TeacherCode))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.User.CreatedAt))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Classes != null ? src.Classes.Count : 0))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src => src.Classes != null
                        ? src.Classes.Sum(c => c.Members != null ? c.Members.Count : 0)
                        : 0))
                .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.Classes));

            // ======================================================================
            // SLOT MAPPINGS
            // ======================================================================

            CreateMap<Slot, SlotDto>()
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class != null ? src.Class.ClassCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectName : null))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class != null && src.Class.Teacher != null && src.Class.Teacher.User != null ? src.Class.Teacher.User.FullName : null))
                .ForMember(dest => dest.TimeSlotName, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.Name : null))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.StartTime : (TimeSpan?)null))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot != null ? src.TimeSlot.EndTime : (TimeSpan?)null))
                .ForMember(dest => dest.SubstituteTeacherName, opt => opt.MapFrom(src => src.SubstituteTeacher != null && src.SubstituteTeacher.User != null ? src.SubstituteTeacher.User.FullName : null))
                .ForMember(dest => dest.HasAttendance, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAttendances, opt => opt.Ignore())
                .ForMember(dest => dest.PresentCount, opt => opt.Ignore())
                .ForMember(dest => dest.AbsentCount, opt => opt.Ignore());

            // ======================================================================
            // CLASS MAPPINGS
            // ======================================================================

            CreateMap<Class, ClassDto>()
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectCode : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.Credits : 0))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null && src.Teacher.User != null ? src.Teacher.User.FullName : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.TeacherCode : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Semester != null ? src.Subject.Semester.Name : null))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src => src.Slots != null ? src.Slots.Count : 0));

            CreateMap<Class, ClassDetailDto>()
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectCode : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.Credits : 0))
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherUserId))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null && src.Teacher.User != null ? src.Teacher.User.FullName : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.TeacherCode : null))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src => src.Teacher != null && src.Teacher.User != null ? src.Teacher.User.Email : null))
                .ForMember(dest => dest.TeacherPhone, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.PhoneNumber : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Semester != null ? src.Subject.Semester.Name : null))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Semester != null ? src.Subject.Semester.StartDate : DateTime.MinValue))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Semester != null ? src.Subject.Semester.EndDate : DateTime.MinValue))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.ApprovedEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count(e => e.IsApproved) : 0))
                .ForMember(dest => dest.PendingEnrollments, opt => opt.MapFrom(src => src.Enrolls != null ? src.Enrolls.Count(e => !e.IsApproved) : 0))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src => src.Slots != null ? src.Slots.Count : 0))
                .ForMember(dest => dest.CompletedSlots, opt => opt.MapFrom(src => src.Slots != null ? src.Slots.Count(s => s.Status == "Completed") : 0))
                .ForMember(dest => dest.ScheduledSlots, opt => opt.MapFrom(src => src.Slots != null ? src.Slots.Count(s => s.Status == "Scheduled") : 0));

            // ======================================================================
            // ATTENDANCE MAPPINGS
            // ======================================================================

            CreateMap<Attendance, AttendanceDto>()
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Slot != null ? src.Slot.Date : DateTime.MinValue))
                .ForMember(dest => dest.TimeSlotName, opt => opt.MapFrom(src => src.Slot != null && src.Slot.TimeSlot != null ? src.Slot.TimeSlot.Name : null))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Slot != null && src.Slot.Class != null ? src.Slot.Class.ClassCode : null));

            CreateMap<Attendance, AttendanceDetailDto>()
                .IncludeBase<Attendance, AttendanceDto>()
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.Email : null))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Slot != null && src.Slot.Class != null && src.Slot.Class.Teacher != null && src.Slot.Class.Teacher.User != null ? src.Slot.Class.Teacher.User.FullName : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject != null && src.Subject.Semester != null ? src.Subject.Semester.Name : null))
                .ForMember(dest => dest.SlotStatus, opt => opt.MapFrom(src => src.Slot != null ? src.Slot.Status : null));

            // ======================================================================
            // ENROLLMENT MAPPINGS
            // ======================================================================

            CreateMap<Enroll, EnrollmentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : null))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.Email : null))
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class != null ? src.Class.ClassCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectName : null))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectCode : null))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved));

            CreateMap<Enroll, EnrollmentDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : null))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.Email : null))
                .ForMember(dest => dest.StudentPhone, opt => opt.Ignore())
                .ForMember(dest => dest.StudentGPA, opt => opt.MapFrom(src => src.Student != null ? src.Student.GPA : 0m))
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class != null ? src.Class.ClassCode : null))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.Class != null ? src.Class.SubjectId : Guid.Empty))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectName : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.Credits : 0))
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.Class != null ? src.Class.TeacherUserId : Guid.Empty))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class != null && src.Class.Teacher != null && src.Class.Teacher.User != null ? src.Class.Teacher.User.FullName : null))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src => src.Class != null && src.Class.Teacher != null && src.Class.Teacher.User != null ? src.Class.Teacher.User.Email : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.Name : null))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.StartDate : DateTime.MinValue))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.EndDate : DateTime.MinValue));

            CreateMap<Enroll, StudentEnrollmentHistoryDto>()
                .ForMember(dest => dest.EnrollmentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class != null ? src.Class.ClassCode : null))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.SubjectName : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null ? src.Class.Subject.Credits : 0))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class != null && src.Class.Teacher != null && src.Class.Teacher.User != null ? src.Class.Teacher.User.FullName : null))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.Name : null))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.StartDate : DateTime.MinValue))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Class != null && src.Class.Subject != null && src.Class.Subject.Semester != null ? src.Class.Subject.Semester.EndDate : DateTime.MinValue));

            // ======================================================================
            // GRADE MAPPINGS
            // ======================================================================

            CreateMap<Grade, GradeDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : null))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
                .ForMember(dest => dest.GradeComponentId, opt => opt.MapFrom(src => src.GradeComponentId))
                .ForMember(dest => dest.ComponentName, opt => opt.MapFrom(src => src.GradeComponent != null ? src.GradeComponent.Name : null))
                .ForMember(dest => dest.ComponentWeight, opt => opt.MapFrom(src => src.GradeComponent != null ? src.GradeComponent.WeightPercent : 0))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.LetterGrade, opt => opt.MapFrom(src => src.LetterGrade))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<Grade, GradeDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.LetterGrade, opt => opt.MapFrom(src => src.LetterGrade))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : null))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.Email : null))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectCode : null))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.Credits : 0))
                .ForMember(dest => dest.GradeComponentId, opt => opt.MapFrom(src => src.GradeComponentId))
                .ForMember(dest => dest.ComponentName, opt => opt.MapFrom(src => src.GradeComponent != null ? src.GradeComponent.Name : null))
                .ForMember(dest => dest.ComponentWeight, opt => opt.MapFrom(src => src.GradeComponent != null ? src.GradeComponent.WeightPercent : 0))
                .ForMember(dest => dest.ClassName, opt => opt.Ignore())
                .ForMember(dest => dest.TeacherName, opt => opt.Ignore());

            // ======================================================================
            // GRADE COMPONENT MAPPINGS
            // ======================================================================

            CreateMap<GradeComponent, GradeComponentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.WeightPercent, opt => opt.MapFrom(src => src.WeightPercent))
                .ForMember(dest => dest.GradeCount, opt => opt.MapFrom(src => src.Grades != null ? src.Grades.Count : 0));

            // ======================================================================
            // SEMESTER MAPPINGS
            // ======================================================================

            CreateMap<Semester, SemesterDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.TotalSubjects, opt => opt.MapFrom(src => src.Subjects != null ? src.Subjects.Count : 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsClosed, opt => opt.MapFrom(src => src.IsClosed));

            CreateMap<Semester, SemesterDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsClosed, opt => opt.MapFrom(src => src.IsClosed))
                .ForMember(dest => dest.TotalSubjects, opt => opt.MapFrom(src => src.Subjects != null ? src.Subjects.Count : 0))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Subjects != null
                        ? src.Subjects.Sum(s => s.Classes != null ? s.Classes.Count : 0)
                        : 0))
                .ForMember(dest => dest.TotalStudentsEnrolled, opt => opt.MapFrom(src => src.Subjects != null
                        ? src.Subjects.SelectMany(s => s.Classes ?? new List<Class>())
                            .SelectMany(c => c.Members ?? new List<ClassMember>())
                            .Select(m => m.StudentId)
                            .Distinct()
                            .Count()
                        : 0))
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(src => src.Subjects));

            // ======================================================================
            // SUBJECT MAPPINGS
            // ======================================================================

            CreateMap<Subject, SubjectDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.SubjectCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.SubjectName))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Credits))
                .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.SemesterId))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Classes != null ? src.Classes.Count : 0));

            CreateMap<Subject, SubjectDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.SubjectCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.SubjectName))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Credits))
                .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.SemesterId))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.StartDate : DateTime.MinValue))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.EndDate : DateTime.MinValue))
                .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.Classes))
                .ForMember(dest => dest.TotalStudentsEnrolled, opt => opt.MapFrom(src => src.Classes != null
                        ? src.Classes.SelectMany(c => c.Members ?? new List<ClassMember>())
                            .Select(m => m.StudentId)
                            .Distinct()
                            .Count()
                        : 0));

            CreateMap<Subject, SubjectSummaryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.SubjectCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.SubjectName))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Credits))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Classes != null ? src.Classes.Count : 0));

            // ======================================================================
            // CLASS SUMMARY MAPPINGS
            // ======================================================================

            CreateMap<Class, ClassSummaryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null && src.Teacher.User != null ? src.Teacher.User.FullName : null))
                .ForMember(dest => dest.CurrentEnrollment, opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.MaxEnrollment, opt => opt.MapFrom(src => src.MaxEnrollment));

            // ======================================================================
            // TIMESLOT MAPPINGS
            // ======================================================================

            CreateMap<TimeSlot, TimeSlotDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => (int)(src.EndTime - src.StartTime).TotalMinutes))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src => src.Slots != null ? src.Slots.Count : 0));
        }
    }
}
