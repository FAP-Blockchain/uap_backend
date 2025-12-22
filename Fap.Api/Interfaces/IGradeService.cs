using Fap.Domain.DTOs;
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

        // ===== ON-CHAIN (GradeManagement) =====
        /// <summary>
        /// Chuẩn bị payload để FE gọi GradeManagement.recordGrade(...)
        /// </summary>
        Task<GradeOnChainPrepareDto?> PrepareGradeOnChainAsync(Guid gradeId);

        /// <summary>
        /// Lưu thông tin transaction on-chain của grade (recordGrade/updateGrade/...)
        /// </summary>
        Task<ServiceResult<bool>> SaveGradeOnChainAsync(Guid gradeId, SaveGradeOnChainRequest request, Guid performedByUserId);

        /// <summary>
        /// Verify all grades of a student in a class by comparing DB with on-chain records.
        /// </summary>
        Task<List<GradeVerifyItemDto>> VerifyGradeListAsync(Guid studentId, Guid classId);
    }
}
