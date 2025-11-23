using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Grade;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface IGradeService
    {
        Task<GradeResponse> CreateGradeAsync(CreateGradeRequest request);

        Task<BulkGradeResponse> CreateGradesAsync(BulkCreateGradesRequest request);

        Task<GradeDetailDto?> GetGradeByIdAsync(Guid id);
        
        Task<GradeResponse> UpdateGradeAsync(Guid id, UpdateGradeRequest request);

        Task<BulkGradeResponse> UpdateGradesAsync(BulkUpdateGradesRequest request);
        
        Task<ClassGradeReportDto?> GetClassGradesAsync(Guid classId, GetClassGradesRequest request);
   
        Task<StudentGradeTranscriptDto?> GetStudentGradesAsync(Guid studentId, GetStudentGradesRequest request);

        Task<PagedResult<GradeDto>> GetAllGradesAsync(GetGradesRequest request);
    }
}
