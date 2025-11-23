using Fap.Domain.Entities;

namespace Fap.Domain.Repositories
{
    public interface IStudentRoadmapRepository : IGenericRepository<StudentRoadmap>
    {
        /// <summary>
        /// Get complete roadmap for a student
        /// </summary>
        Task<List<StudentRoadmap>> GetStudentRoadmapAsync(Guid studentId);

        /// <summary>
        /// Get roadmap for a student in a specific semester
        /// </summary>
        Task<List<StudentRoadmap>> GetRoadmapBySemesterAsync(Guid studentId, Guid semesterId);

        /// <summary>
        /// Get roadmap with full details (Subject, Semester)
        /// </summary>
        Task<StudentRoadmap?> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// Get current semester roadmap for a student
        /// </summary>
        Task<List<StudentRoadmap>> GetCurrentSemesterRoadmapAsync(Guid studentId);

        /// <summary>
        /// Get subjects student should enroll next (status = "Planned")
        /// </summary>
        Task<List<StudentRoadmap>> GetPlannedSubjectsAsync(Guid studentId, Guid? semesterId = null);

        /// <summary>
        /// Get subjects ready to enroll (status = "Open" - prerequisites met, not yet enrolled)
        /// </summary>
        Task<List<StudentRoadmap>> GetOpenSubjectsAsync(Guid studentId);

        /// <summary>
        /// Get completed subjects (status = "Completed")
        /// </summary>
        Task<List<StudentRoadmap>> GetCompletedSubjectsAsync(Guid studentId);

        /// <summary>
        /// Get in-progress subjects (status = "InProgress")
        /// </summary>
        Task<List<StudentRoadmap>> GetInProgressSubjectsAsync(Guid studentId);

        /// <summary>
        /// Check if student already has a roadmap entry for a subject
        /// </summary>
        Task<bool> HasRoadmapEntryAsync(Guid studentId, Guid subjectId);

        /// <summary>
        /// Get roadmap entry by student and subject
        /// </summary>
        Task<StudentRoadmap?> GetByStudentAndSubjectAsync(Guid studentId, Guid subjectId);

        /// <summary>
        /// Update roadmap status based on enrollment/grade
        /// </summary>
        Task UpdateRoadmapStatusAsync(Guid studentId, Guid subjectId, string status, decimal? finalScore = null, string? letterGrade = null);

        /// <summary>
        /// Update roadmap status and semester when student enrolls in a class
        /// </summary>
        Task UpdateRoadmapOnEnrollmentAsync(Guid studentId, Guid subjectId, Guid actualSemesterId);

        /// <summary>
        /// Get roadmap statistics for a student
        /// </summary>
        Task<(int Total, int Completed, int InProgress, int Planned, int Failed)> GetRoadmapStatisticsAsync(Guid studentId);

        /// <summary>
        /// Get paginated roadmap with filters
        /// </summary>
        Task<(List<StudentRoadmap> Roadmaps, int TotalCount)> GetPagedRoadmapAsync(
        Guid studentId,
                 int page,
           int pageSize,
                   string? status = null,
                   Guid? semesterId = null,
             string? sortBy = null,
                   string? sortOrder = null);
    }
}
