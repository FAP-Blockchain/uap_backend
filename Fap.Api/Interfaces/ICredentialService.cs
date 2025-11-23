using Fap.Domain.DTOs.Credential;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs; // For ServiceResult
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface ICredentialService
    {
        // ==================== CREDENTIALS ====================

        Task<PagedResult<CredentialDto>> GetCredentialsAsync(GetCredentialsRequest request);
        Task<CredentialDetailDto?> GetCredentialByIdAsync(Guid id);
        Task<CredentialDetailDto> CreateCredentialAsync(CreateCredentialRequest request, Guid createdBy);
        Task<ServiceResult<CredentialDetailDto>> IssueCredentialAsync(IssueCredentialDto request); // ✅ NEW - Admin Issue Credential
        Task<CredentialDetailDto> ReviewCredentialAsync(Guid credentialId, ReviewCredentialRequest request, Guid reviewedBy);
        Task RevokeCredentialAsync(Guid credentialId, RevokeCredentialRequest request, Guid revokedBy);

        // Credential Operations
        Task<(byte[] FileBytes, string FileName)> GenerateCredentialPdfAsync(Guid credentialId);
        Task<string> GenerateQRCodeAsync(Guid credentialId, Guid? userId, int size = 300);
        Task<CredentialShareDto> GetCredentialShareInfoAsync(Guid credentialId, Guid? userId);
        Task<CredentialVerificationDto> VerifyCredentialAsync(VerifyCredentialRequest request);

        // ==================== PUBLIC VIEW (No Auth Required) ====================
        
        /// <summary>
        /// Lấy thông tin chứng chỉ công khai (dành cho người xem qua QR/Link)
        /// </summary>
        Task<CertificatePublicDto?> GetPublicCertificateAsync(Guid credentialId);
        
        /// <summary>
        /// Lấy danh sách chứng chỉ của sinh viên để chia sẻ (kèm QR + URL)
        /// </summary>
        Task<List<CertificatePublicDto>> GetMyCertificatesForSharingAsync(Guid userId);

        // ==================== STUDENT OPERATIONS ====================

        Task<List<CredentialDto>> GetStudentCredentialsAsync(Guid userId, string? certificateType = null);
        Task<List<CredentialDto>> GetStudentCredentialsByStudentIdAsync(Guid studentId, string? certificateType = null);
        Task<StudentCredentialSummaryDto> GetStudentCredentialSummaryAsync(Guid userId);
        Task<Guid?> GetStudentIdByUserIdAsync(Guid userId);

        // ==================== CREDENTIAL REQUESTS ====================

        Task<PagedResult<CredentialRequestDto>> GetCredentialRequestsAsync(GetCredentialRequestsRequest request);
        Task<CredentialRequestDto?> GetCredentialRequestByIdAsync(Guid id);
        Task<CredentialRequestDto> RequestCredentialAsync(Guid userId, RequestCredentialRequest request);
        Task<List<CredentialRequestDto>> GetStudentCredentialRequestsAsync(Guid userId, string? status = null);
        Task<CredentialDetailDto?> ProcessCredentialRequestAsync(Guid requestId, ProcessCredentialRequestRequest request, Guid processedBy);

        // ==================== AUTO-GENERATION (Background Jobs) ====================

        Task<CredentialRequestDto?> AutoRequestSubjectCompletionCredentialAsync(Guid studentId, Guid subjectId);
        // REMOVED: Semester Completion - No longer supported
        // Task<CredentialRequestDto?> AutoRequestSemesterCompletionCredentialAsync(Guid studentId, Guid semesterId);
        Task<CredentialRequestDto?> AutoRequestRoadmapCompletionCredentialAsync(Guid studentId, Guid roadmapId);

        // ==================== TEMPLATES ====================

        Task<List<CertificateTemplateDto>> GetTemplatesAsync(string? templateType = null, bool includeInactive = false);
        Task<List<CertificateTemplateDto>> GetSampleTemplatesAsync(string? templateType = null);
        Task<CertificateTemplateDto?> GetTemplateByIdAsync(Guid id);
        Task<CertificateTemplateDto> CreateTemplateAsync(CreateCertificateTemplateRequest request);
        Task<CertificateTemplateDto> UpdateTemplateAsync(Guid id, UpdateCertificateTemplateRequest request);
        Task DeleteTemplateAsync(Guid id);
        Task<(byte[] FileBytes, string FileName)> PreviewTemplateAsync(Guid templateId);
    }
}
