using Fap.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Domain.Repositories
{
    public interface IGradeRepository : IGenericRepository<Grade>
    {
        Task<Grade?> GetByIdWithDetailsAsync(Guid id);
        
        Task<List<Grade>> GetGradesByStudentIdAsync(Guid studentId);
        
        Task<List<Grade>> GetGradesByStudentAndSubjectAsync(Guid studentId, Guid subjectId);
        
        Task<List<Grade>> GetGradesByClassIdAsync(Guid classId);
        
        Task<List<Grade>> GetGradesBySubjectIdAsync(Guid subjectId);
        
        Task<Grade?> GetGradeByStudentSubjectComponentAsync(
            Guid studentId, 
            Guid subjectId, 
            Guid gradeComponentId);
        
        Task<List<Grade>> GetStudentGradesBySemesterAsync(Guid studentId, Guid semesterId);
        
        Task<bool> HasGradesAsync(Guid studentId, Guid subjectId);
    
        Task<decimal?> CalculateAverageScoreAsync(Guid studentId, Guid subjectId);
    }
}
