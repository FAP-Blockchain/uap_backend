using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.StudentRoadmap;

namespace Fap.Api.Interfaces
{
    public interface IStudentRoadmapService
    {
        // ==================== STUDENT APIs ====================

        /// <summary>
        /// Get complete roadmap overview for a student (used by student to view their own roadmap)
        /// </summary>
        Task<StudentRoadmapOverviewDto?> GetMyRoadmapAsync(Guid studentId);

        /// <summary>
        /// Get roadmap for specific semester
        /// </summary>
        Task<List<StudentRoadmapDto>> GetRoadmapBySemesterAsync(Guid studentId, Guid semesterId);

        /// <summary>
        /// Get current semester roadmap
        /// </summary>
        Task<List<StudentRoadmapDto>> GetCurrentSemesterRoadmapAsync(Guid studentId);

        /// <summary>
        /// Get recommended subjects for next enrollment
        /// </summary>
        Task<List<RecommendedSubjectDto>> GetRecommendedSubjectsAsync(Guid studentId);

        /// <summary>
        /// Get paginated roadmap with filters
        /// </summary>
        Task<PagedResult<StudentRoadmapDto>> GetPagedRoadmapAsync(Guid studentId, GetStudentRoadmapRequest request);

        // ==================== ADMIN APIs ====================

        /// <summary>
        /// Get roadmap details by ID (admin)
        /// </summary>
        Task<StudentRoadmapDetailDto?> GetRoadmapByIdAsync(Guid id);

        /// <summary>
        /// Create roadmap entry for a student (admin)
        /// </summary>
        Task<StudentRoadmapResponse> CreateRoadmapAsync(CreateStudentRoadmapRequest request);

        /// <summary>
        /// Update roadmap entry (admin or auto-update from grades)
        /// </summary>
        Task<StudentRoadmapResponse> UpdateRoadmapAsync(Guid id, UpdateStudentRoadmapRequest request);

        /// <summary>
        /// Delete roadmap entry (admin)
        /// </summary>
        Task<StudentRoadmapResponse> DeleteRoadmapAsync(Guid id);

        /// <summary>
        /// Bulk create roadmap for a student from template
        /// </summary>
        Task<StudentRoadmapResponse> CreateRoadmapFromTemplateAsync(Guid studentId, List<CreateStudentRoadmapRequest> roadmapItems);

        // ==================== AUTOMATION ====================

        /// <summary>
        /// Update roadmap status when student enrolls in a class
        /// </summary>
        Task UpdateRoadmapOnEnrollmentAsync(Guid studentId, Guid subjectId);

        /// <summary>
        /// Update roadmap status when grade is finalized
        /// </summary>
        Task UpdateRoadmapOnGradeAsync(Guid studentId, Guid subjectId, decimal finalScore, string letterGrade);
    }
}
