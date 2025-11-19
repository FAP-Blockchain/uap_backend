using Fap.Domain.Entities;
using Fap.Domain.Repositories;

namespace Fap.Domain.Repositories
{
    public interface IClassMemberRepository : IGenericRepository<ClassMember>
    {
        /// <summary>
        /// Get all class members for a specific class
        /// </summary>
        Task<List<ClassMember>> GetByClassIdAsync(Guid classId);

        /// <summary>
        /// Get class members with full student details (for admin display)
        /// </summary>
        Task<List<ClassMember>> GetClassMembersWithDetailsAsync(Guid classId);

        /// <summary>
        /// Get all classes a student is enrolled in
        /// </summary>
        Task<List<ClassMember>> GetByStudentIdAsync(Guid studentId);

        /// <summary>
        /// Check if a student is already in a class roster
        /// </summary>
        Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId);

        /// <summary>
        /// Get class member count for a specific class
        /// </summary>
        Task<int> GetClassMemberCountAsync(Guid classId);

        /// <summary>
        /// Remove student from class roster
        /// </summary>
        Task RemoveStudentFromClassAsync(Guid classId, Guid studentId);
    }
}
