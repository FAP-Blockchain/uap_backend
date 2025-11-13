using Fap.Domain.DTOs.Grade;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IGradeService
    {
        Task<GradeResponse> CreateGradeAsync(CreateGradeRequest request);

        Task<GradeDetailDto?> GetGradeByIdAsync(Guid id);
        
        Task<GradeResponse> UpdateGradeAsync(Guid id, UpdateGradeRequest request);
        
        Task<ClassGradeReportDto?> GetClassGradesAsync(Guid classId, GetClassGradesRequest request);
   
        Task<StudentGradeTranscriptDto?> GetStudentGradesAsync(Guid studentId, GetStudentGradesRequest request);
    }
}
