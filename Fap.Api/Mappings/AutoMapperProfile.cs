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
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src =>
                    src.Student != null ? src.Student.StudentCode : null))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src =>
                    src.Teacher != null ? src.Teacher.TeacherCode : null));

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
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src =>
                    src.ClassMembers != null ? src.ClassMembers.Count : 0));

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
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.ApprovedEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count(e => e.IsApproved) : 0))
                .ForMember(dest => dest.PendingEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count(e => !e.IsApproved) : 0))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src =>
                    src.ClassMembers != null ? src.ClassMembers.Count : 0))
                .ForMember(dest => dest.TotalGrades, opt => opt.MapFrom(src =>
                    src.Grades != null ? src.Grades.Count : 0))
                .ForMember(dest => dest.TotalAttendances, opt => opt.MapFrom(src =>
                    src.Attendances != null ? src.Attendances.Count : 0))
                .ForMember(dest => dest.Enrollments, opt => opt.MapFrom(src => src.Enrolls))
                .ForMember(dest => dest.CurrentClasses, opt => opt.MapFrom(src => src.ClassMembers));

            // Enroll -> EnrollmentInfo
            CreateMap<Enroll, EnrollmentInfo>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class.Subject.SubjectName))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class.Teacher.User.FullName))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved));

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
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src =>
                    src.Classes != null ? src.Classes.Count : 0));

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
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src =>
                    src.Classes != null ? src.Classes.Count : 0))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src =>
                    src.Classes != null
                        ? src.Classes.Sum(c => c.Members != null ? c.Members.Count : 0)
                        : 0))
                .ForMember(dest => dest.Classes, opt => opt.MapFrom(src => src.Classes));

            CreateMap<Class, TeachingClassInfo>()
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject.SubjectCode))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject.Credits))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject.Semester.Name))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count : 0));

            // ======================================================================
            // CLASS MAPPINGS
            // ======================================================================

            CreateMap<Class, ClassDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject.SubjectCode))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject.Credits))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.User.FullName))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.Teacher.TeacherCode))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject.Semester.Name))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count : 0));

            CreateMap<Class, ClassDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.ClassCode))
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject.SubjectCode))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Subject.Credits))
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherUserId))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.User.FullName))
                .ForMember(dest => dest.TeacherCode, opt => opt.MapFrom(src => src.Teacher.TeacherCode))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src => src.Teacher.User.Email))
                .ForMember(dest => dest.TeacherPhone, opt => opt.MapFrom(src => src.Teacher.PhoneNumber))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Subject.Semester.Name))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Subject.Semester.StartDate))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Subject.Semester.EndDate))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src =>
                    src.Members != null ? src.Members.Count : 0))
                .ForMember(dest => dest.TotalEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count : 0))
                .ForMember(dest => dest.ApprovedEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count(e => e.IsApproved) : 0))
                .ForMember(dest => dest.PendingEnrollments, opt => opt.MapFrom(src =>
                    src.Enrolls != null ? src.Enrolls.Count(e => !e.IsApproved) : 0))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count : 0))
                .ForMember(dest => dest.CompletedSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count(s => s.Status == "Completed") : 0))
                .ForMember(dest => dest.ScheduledSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count(s => s.Status == "Scheduled") : 0))
                .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.Members))
                .ForMember(dest => dest.Enrollments, opt => opt.MapFrom(src => src.Enrolls))
                .ForMember(dest => dest.Slots, opt => opt.MapFrom(src => src.Slots));

            // ClassMember -> ClassStudentInfo
            CreateMap<ClassMember, ClassStudentInfo>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Student.User.Email))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src => src.Student.GPA))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt));

            // ======================================================================
            // ENROLLMENT MAPPINGS
            // ======================================================================

            CreateMap<Enroll, EnrollmentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student.User.Email))
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class.Subject.SubjectName))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class.Subject.SubjectCode))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved));

            CreateMap<Enroll, EnrollmentDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))

                // Student
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student.User.Email))
                .ForMember(dest => dest.StudentPhone, opt => opt.MapFrom(src => "N/A"))
                .ForMember(dest => dest.StudentGPA, opt => opt.MapFrom(src => src.Student.GPA))

                // Class
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode))

                // Subject
                .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.Class.SubjectId))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class.Subject.SubjectCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class.Subject.SubjectName))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Class.Subject.Credits))

                // Teacher
                .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.Class.TeacherUserId))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class.Teacher.User.FullName))
                .ForMember(dest => dest.TeacherEmail, opt => opt.MapFrom(src => src.Class.Teacher.User.Email))

                // Semester
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Class.Subject.Semester.Name))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Class.Subject.Semester.StartDate))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Class.Subject.Semester.EndDate));

            CreateMap<Enroll, StudentEnrollmentHistoryDto>()
                .ForMember(dest => dest.EnrollmentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ClassId, opt => opt.MapFrom(src => src.ClassId))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode))
                .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Class.Subject.SubjectCode))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Class.Subject.SubjectName))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => src.Class.Subject.Credits))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class.Teacher.User.FullName))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Class.Subject.Semester.Name))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.RegisteredAt))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))
                .ForMember(dest => dest.SemesterStartDate, opt => opt.MapFrom(src => src.Class.Subject.Semester.StartDate))
                .ForMember(dest => dest.SemesterEndDate, opt => opt.MapFrom(src => src.Class.Subject.Semester.EndDate));

            // ======================================================================
            // TIMESLOT
            // ======================================================================

            CreateMap<TimeSlot, TimeSlotDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\\:mm")))
                .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src =>
                    (int)(src.EndTime - src.StartTime).TotalMinutes))
                .ForMember(dest => dest.TotalSlots, opt => opt.MapFrom(src =>
                    src.Slots != null ? src.Slots.Count : 0));

            // ======================================================================
            // SUBJECT
            // ======================================================================

            CreateMap<Subject, SubjectDto>()
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(dest => dest.TotalClasses, opt => opt.MapFrom(src => src.Classes.Count));

            CreateMap<CreateSubjectRequest, Subject>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Semester, opt => opt.Ignore())
                .ForMember(dest => dest.Classes, opt => opt.Ignore())
                .ForMember(dest => dest.Slots, opt => opt.Ignore())
                .ForMember(dest => dest.Grades, opt => opt.Ignore())
                .ForMember(dest => dest.Roadmaps, opt => opt.Ignore())
                .ForMember(dest => dest.SubjectCriterias, opt => opt.Ignore());

            // ======================================================================
            // SEMESTER
            // ======================================================================

            CreateMap<Semester, SemesterDto>()
                .ForMember(dest => dest.TotalSubjects, opt => opt.MapFrom(src => src.Subjects.Count))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src =>
                    src.StartDate <= DateTime.UtcNow && src.EndDate >= DateTime.UtcNow));

            CreateMap<CreateSemesterRequest, Semester>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsClosed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Subjects, opt => opt.Ignore());
        }
    }
}
