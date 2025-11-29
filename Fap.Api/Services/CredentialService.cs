using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Credential;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs; // For ServiceResult
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Fap.Api.Services
{
    public class CredentialService : ICredentialService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CredentialService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FrontendSettings _frontendSettings;
        private readonly IBlockchainService _blockchainService;
        private readonly IPdfService _pdfService;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IIpfsService _ipfsService;

        public CredentialService(
             IUnitOfWork uow,
               IMapper mapper,
                    ILogger<CredentialService> logger,
          IConfiguration configuration,
          IOptions<FrontendSettings> frontendOptions,
          IBlockchainService blockchainService,
          IPdfService pdfService,
                    ICloudStorageService cloudStorageService,
                    IIpfsService ipfsService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _frontendSettings = frontendOptions.Value;
            _blockchainService = blockchainService;
            _pdfService = pdfService;
            _cloudStorageService = cloudStorageService;
                        _ipfsService = ipfsService;
        }

        // ==================== ADMIN ISSUE CREDENTIAL ====================

        public async Task<ServiceResult<CredentialDetailDto>> IssueCredentialAsync(IssueCredentialDto request)
        {
            try
            {
                // 1. Validate Student
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null) return ServiceResult<CredentialDetailDto>.Fail("Student not found");

                // 2. Validate & Prepare Data based on Type
                Credential credential = new Credential
                {
                    StudentId = request.StudentId,
                    IssuedDate = DateTime.UtcNow,
                    Status = "Issued", // Use string literal as CredentialStatus enum might not be available or is string
                    CertificateType = request.Type,
                    CredentialId = "", // Will be set later
                    IPFSHash = "", // Will be set later if needed
                    FileUrl = "", // Will be set later
                    CertificateTemplateId = Guid.Empty // Will be set later
                };

                CertificateTemplate? template = null;

                if (request.Type == "SubjectCompletion")
                {
                    if (!request.SubjectId.HasValue)
                        return ServiceResult<CredentialDetailDto>.Fail("SubjectId is required for SubjectCompletion");

                    // Check if already exists
                    var existingCredentials = await _uow.Credentials.FindAsync(c => 
                        c.StudentId == request.StudentId && 
                        c.SubjectId == request.SubjectId && 
                        !c.IsRevoked);
                    
                    if (existingCredentials.Any()) return ServiceResult<CredentialDetailDto>.Fail("Credential already exists for this subject");

                    // Get Grade info
                    var grades = await _uow.Grades.GetGradesByStudentAndSubjectAsync(request.StudentId, request.SubjectId.Value);
                    var grade = grades.OrderByDescending(g => g.Score).FirstOrDefault(); // Get highest grade if multiple
                    
                    if (grade == null || grade.Score < 5.0m) // Assuming 5.0 is pass
                        return ServiceResult<CredentialDetailDto>.Fail("Student has not passed this subject or grade not found");
                    
                    credential.SubjectId = request.SubjectId;
                    credential.FinalGrade = grade.Score;
                    credential.LetterGrade = grade.LetterGrade;
                    credential.CompletionDate = DateTime.UtcNow; // Or get from semester end date
                    
                    var templates = await _uow.CertificateTemplates.FindAsync(t => t.TemplateType == "SubjectCompletion" && t.IsActive);
                    template = templates.FirstOrDefault();
                }
                else if (request.Type == "RoadmapCompletion")
                {
                     if (!request.StudentRoadmapId.HasValue)
                        return ServiceResult<CredentialDetailDto>.Fail("StudentRoadmapId is required for RoadmapCompletion");
                     
                     // Check if already exists
                     var existingCredentials = await _uow.Credentials.FindAsync(c => 
                        c.StudentId == request.StudentId && 
                        c.StudentRoadmapId == request.StudentRoadmapId && 
                        !c.IsRevoked);
                    
                    if (existingCredentials.Any()) return ServiceResult<CredentialDetailDto>.Fail("Credential already exists for this roadmap");

                     credential.StudentRoadmapId = request.StudentRoadmapId;
                     // Additional logic to get roadmap details/GPA if needed
                     
                     var templates = await _uow.CertificateTemplates.FindAsync(t => t.TemplateType == "RoadmapCompletion" && t.IsActive);
                     template = templates.FirstOrDefault();
                }

                if (template == null)
                    return ServiceResult<CredentialDetailDto>.Fail("Active certificate template not found");

                credential.CertificateTemplateId = template.Id;

                // 3. Generate Core Data (Number, Hash)
                credential.CredentialId = await GenerateCredentialNumberAsync(request.Type);
                credential.VerificationHash = GenerateVerificationHash(credential.CredentialId, credential.StudentId);

                // 4. Save Initial Credential to get ID
                await _uow.Credentials.AddAsync(credential);
                await _uow.SaveChangesAsync();

                // 5. Generate & upload PDF (cloud + IPFS)
                byte[]? pdfBytes = null;
                string? pdfCloudUrl = null;
                string? ipfsCid = null;

                try
                {
                    var fullCredential = await _uow.Credentials.GetByIdAsync(credential.Id);

                    if (fullCredential != null)
                    {
                        pdfBytes = await _pdfService.GenerateCertificatePdfAsync(fullCredential);
                        var fileName = $"{credential.CredentialId}.pdf";

                        pdfCloudUrl = await _cloudStorageService.UploadPdfAsync(pdfBytes, fileName);

                        credential.PdfUrl = pdfCloudUrl;
                        credential.PdfFilePath = $"cloudinary://{fileName}";
                        credential.UpdatedAt = DateTime.UtcNow;

                        // Also upload to IPFS via Pinata (best-effort)
                        try
                        {
                            ipfsCid = await _ipfsService.UploadBytesAsync(pdfBytes, fileName);
                            var ipfsUrl = _ipfsService.GetFileUrl(ipfsCid);

                            credential.IPFSHash = ipfsCid;
                            credential.FileUrl = ipfsUrl;

                            _logger.LogInformation("Uploaded credential PDF to IPFS. CredentialId={CredentialId}, CID={Cid}, Url={Url}",
                                credential.Id, ipfsCid, ipfsUrl);
                        }
                        catch (Exception ipfsEx)
                        {
                            _logger.LogError(ipfsEx,
                                "Failed to upload credential PDF to IPFS. CredentialId={CredentialId}",
                                credential.Id);
                        }

                        _uow.Credentials.Update(credential);
                        await _uow.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Unable to load credential {CredentialId} for PDF generation", credential.Id);
                    }
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx, "Error generating PDF for issued credential {Id}", credential.Id);
                    // Không fail toàn bộ, admin có thể xử lý lại
                }

                // 6. Chuẩn bị metadata JSON cho on-chain
                string ipfsCidForMetadata = credential.IPFSHash ?? ipfsCid ?? string.Empty;
                string fileUrl = credential.FileUrl ?? pdfCloudUrl ?? string.Empty;
                string verificationHash = credential.VerificationHash ?? string.Empty;

                var metadata = new
                {
                    cid = ipfsCidForMetadata,
                    fileUrl,
                    verificationHash
                };

                string credentialDataJson = JsonSerializer.Serialize(metadata);

                // 7. Gọi smart contract CredentialManagement.issueCredential
                try
                {
                    var studentUser = student.User;
                    if (studentUser == null || string.IsNullOrWhiteSpace(studentUser.WalletAddress))
                    {
                        return ServiceResult<CredentialDetailDto>.Fail("Student has no blockchain wallet address configured");
                    }

                    var credentialTypeOnChain = request.Type;
                    ulong expiresAtUnix = 0;

                    var (onChainId, txHash) = await _blockchainService.IssueCredentialOnChainAsync(
                        studentUser.WalletAddress,
                        credentialTypeOnChain,
                        credentialDataJson,
                        expiresAtUnix
                    );

                    credential.BlockchainCredentialId = onChainId;
                    credential.BlockchainTransactionHash = txHash;
                    credential.BlockchainStoredAt = DateTime.UtcNow;
                    credential.IsOnBlockchain = true;

                    _uow.Credentials.Update(credential);
                    await _uow.SaveChangesAsync();
                }
                catch (Exception chainEx)
                {
                    _logger.LogError(chainEx, "Error issuing credential on blockchain for credential {Id}", credential.Id);
                    return ServiceResult<CredentialDetailDto>.Fail("Failed to record credential on blockchain");
                }

                // 8. Đảm bảo có ShareableUrl + QRCode ngay sau khi issue
                try
                {
                    await EnsureShareArtifactsAsync(credential);
                }
                catch (Exception shareEx)
                {
                    _logger.LogError(shareEx,
                        "Failed to generate share artifacts (URL/QR) for credential {Id}",
                        credential.Id);
                }

                var dto = _mapper.Map<CredentialDetailDto>(credential);
                return ServiceResult<CredentialDetailDto>.SuccessResponse(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error issuing credential");
                return ServiceResult<CredentialDetailDto>.Fail("Internal server error: " + ex.Message);
            }
        }

        // ==================== AUTO-REQUEST METHODS ====================

        public async Task<CredentialRequestDto?> AutoRequestSubjectCompletionCredentialAsync(Guid studentId, Guid subjectId)
        {
            try
            {
                _logger.LogInformation("Auto-requesting subject completion credential for Student {StudentId}, Subject {SubjectId}",
                            studentId, subjectId);

                // Check if request already exists
                var existingRequests = await _uow.CredentialRequests.FindAsync(r =>
                           r.StudentId == studentId &&
                   r.SubjectId == subjectId &&
                        r.CertificateType == "SubjectCompletion" &&
                    r.Status == "Pending");

                if (existingRequests.Any())
                {
                    _logger.LogInformation("Certificate request already exists");
                    return null;
                }

                // Get final grade
                var grades = await _uow.Grades.FindAsync(g =>
                      g.StudentId == studentId &&
                   g.SubjectId == subjectId);

                var finalGrade = grades
                    .Where(g => g.GradeComponent.Name.Contains("Final", StringComparison.OrdinalIgnoreCase))
                      .OrderByDescending(g => g.UpdatedAt)
                       .FirstOrDefault();

                if (finalGrade == null || finalGrade.Score < 5.0m)
                {
                    _logger.LogInformation("Student has not passed the subject");
                    return null;
                }

                // Create request
                var request = new CredentialRequest
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    CertificateType = "SubjectCompletion",
                    SubjectId = subjectId,
                    Status = "Pending",
                    FinalGrade = finalGrade.Score,
                    LetterGrade = finalGrade.LetterGrade,
                    CompletionDate = DateTime.UtcNow,
                    IsAutoGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.CredentialRequests.AddAsync(request);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Created auto-request {RequestId}", request.Id);
                return _mapper.Map<CredentialRequestDto>(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-requesting subject completion credential");
                return null;
            }
        }

        // REMOVED: Semester Completion - No longer supported
        // public async Task<CredentialRequestDto?> AutoRequestSemesterCompletionCredentialAsync(Guid studentId, Guid semesterId)
        // {
        //     ... removed code ...
        // }

        public async Task<CredentialRequestDto?> AutoRequestRoadmapCompletionCredentialAsync(Guid studentId, Guid roadmapId)
        {
            try
            {
                _logger.LogInformation("Auto-requesting roadmap completion credential for Student {StudentId}, Roadmap {RoadmapId}",
                     studentId, roadmapId);

                // Check existing request
                var existingRequests = await _uow.CredentialRequests.FindAsync(r =>
                           r.StudentId == studentId &&
                      r.StudentRoadmapId == roadmapId &&
                    r.CertificateType == "RoadmapCompletion" &&
                         r.Status == "Pending");

                if (existingRequests.Any())
                {
                    _logger.LogInformation("Certificate request already exists");
                    return null;
                }

                // Get student roadmap
                var roadmaps = await _uow.StudentRoadmaps.FindAsync(sr =>
               sr.StudentId == studentId &&
                      sr.Id == roadmapId);

                var roadmap = roadmaps.FirstOrDefault();
                if (roadmap == null || roadmap.Status != "Completed")
                {
                    _logger.LogInformation("Roadmap not found or not completed");
                    return null;
                }

                // Get student overall GPA
                var student = await _uow.Students.GetByIdAsync(studentId);
                var overallGPA = student?.GPA ?? 0m;

                if (overallGPA < 5.0m)
                {
                    _logger.LogInformation("Student GPA does not meet minimum requirement");
                    return null;
                }

                var classification = overallGPA >= 9.0m ? "First Class Honours" :
overallGPA >= 8.0m ? "Second Class Honours (Upper)" :
      overallGPA >= 7.0m ? "Second Class Honours (Lower)" :
      overallGPA >= 6.0m ? "Third Class Honours" : "Pass";

                // Create request
                var request = new CredentialRequest
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    CertificateType = "RoadmapCompletion",
                    StudentRoadmapId = roadmapId,
                    Status = "Pending",
                    FinalGrade = overallGPA,
                    Classification = classification,
                    CompletionDate = roadmap.CompletedAt ?? DateTime.UtcNow,
                    IsAutoGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.CredentialRequests.AddAsync(request);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Created roadmap completion request {RequestId}", request.Id);
                return _mapper.Map<CredentialRequestDto>(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-requesting roadmap completion credential");
                return null;
            }
        }

        // ==================== CREDENTIAL CRUD ====================

        public async Task<PagedResult<CredentialDto>> GetCredentialsAsync(GetCredentialsRequest request)
        {
            try
            {
                var credentials = await _uow.Credentials.GetAllAsync();

                // Apply filters
                if (request.StudentId.HasValue)
                    credentials = credentials.Where(c => c.StudentId == request.StudentId.Value);

                if (!string.IsNullOrEmpty(request.CertificateType))
                    credentials = credentials.Where(c => c.CertificateType == request.CertificateType);

                if (!string.IsNullOrEmpty(request.Status))
                    credentials = credentials.Where(c => c.Status == request.Status);

                if (request.IssuedFrom.HasValue)
                    credentials = credentials.Where(c => c.IssuedDate >= request.IssuedFrom.Value);

                if (request.IssuedTo.HasValue)
                    credentials = credentials.Where(c => c.IssuedDate <= request.IssuedTo.Value);

                var totalCount = credentials.Count();

                var items = credentials
                  .OrderByDescending(c => c.IssuedDate)
                .Skip((request.Page - 1) * request.PageSize)
           .Take(request.PageSize)
          .ToList();

                var dtos = _mapper.Map<List<CredentialDto>>(items);

                return new PagedResult<CredentialDto>(dtos, totalCount, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials");
                return new PagedResult<CredentialDto>();
            }
        }

        public async Task<CredentialDetailDto?> GetCredentialByIdAsync(Guid id)
        {
            try
            {
                var credential = await _uow.Credentials.GetByIdAsync(id);
                if (credential == null) return null;

                // Increment view count
                credential.ViewCount++;
                credential.LastViewedAt = DateTime.UtcNow;
                _uow.Credentials.Update(credential);
                await _uow.SaveChangesAsync();

                return _mapper.Map<CredentialDetailDto>(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential {CredentialId}", id);
                return null;
            }
        }

        public async Task<CredentialDetailDto> CreateCredentialAsync(CreateCredentialRequest request, Guid createdBy)
        {
            try
            {
                // Validate student
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null)
                    throw new InvalidOperationException($"Student {request.StudentId} not found");

                // Validate template
                var template = await _uow.CertificateTemplates.GetByIdAsync(request.TemplateId);
                if (template == null)
                    throw new InvalidOperationException($"Template {request.TemplateId} not found");

                // Generate unique credential number
                var credentialNumber = await GenerateCredentialNumberAsync(request.CertificateType);
                var verificationHash = GenerateVerificationHash(credentialNumber, request.StudentId);

                // Generate IPFS hash placeholder (will be updated when actually uploaded to IPFS)
                var ipfsHash = $"Qm{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 46);
                
                // Generate shareable URL
                var frontendBaseUrl = _frontendSettings.BaseUrl;
                var shareableUrl = $"{frontendBaseUrl}/public-portal/certificates/verify/{credentialNumber}";

                var credential = new Credential
                {
                    Id = Guid.NewGuid(),
                    CredentialId = credentialNumber,
                    StudentId = request.StudentId,
                    CertificateTemplateId = request.TemplateId,
                    CertificateType = request.CertificateType,
                    SubjectId = request.SubjectId,
                    SemesterId = request.SemesterId,
                    StudentRoadmapId = request.RoadmapId,
                    IssuedDate = DateTime.UtcNow,
                    CompletionDate = request.CompletionDate ?? DateTime.UtcNow,
                    FinalGrade = request.FinalGrade,
                    LetterGrade = request.LetterGrade,
                    Classification = request.Classification,
                    VerificationHash = verificationHash,
                    ShareableUrl = shareableUrl,
                    Status = "Issued",
                    ReviewedBy = createdBy,
                    ReviewedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    
                    // IPFS and File Storage (Required fields)
                    IPFSHash = ipfsHash,
                    FileUrl = $"https://ipfs.io/ipfs/{ipfsHash}",
                    PdfUrl = $"{_frontendSettings.BaseUrl}/api/credentials/{credentialNumber}/download",
                    
                    // QR Code - will be generated lazily on first view
                    QRCodeData = null,
                    
                    // Blockchain - initially not on blockchain (will be issued after creation)
                    IsOnBlockchain = false,
                    BlockchainCredentialId = null,
                    BlockchainTransactionHash = null,
                    BlockchainStoredAt = null,
                    
                    // Metrics
                    ViewCount = 0,
                    LastViewedAt = null,
                    
                    // Revocation
                    IsRevoked = false,
                    RevokedAt = null,
                    RevocationReason = null
                };

                // DEBUG: Log SubjectId value before saving
                _logger.LogWarning("DEBUG - SubjectId value: {SubjectId}, HasValue: {HasValue}, IsEmpty: {IsEmpty}", 
                    credential.SubjectId, 
                    credential.SubjectId.HasValue,
                    credential.SubjectId == Guid.Empty);

                await _uow.Credentials.AddAsync(credential);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Created credential {CredentialId} for student {StudentId}",
                  credential.Id, request.StudentId);

                // Generate PDF and upload to Cloud Storage in background
                try
                {
                    var pdfBytes = await _pdfService.GenerateCertificatePdfAsync(credential);
                    var fileName = $"{credential.CredentialId}.pdf";
                    
                    var cloudUrl = await _cloudStorageService.UploadPdfAsync(pdfBytes, fileName);
                    
                    // Update credential with PDF URL
                    credential.PdfUrl = cloudUrl;
                    credential.PdfFilePath = $"cloudinary://{fileName}";
                    credential.UpdatedAt = DateTime.UtcNow;
                    
                    _uow.Credentials.Update(credential);
                    await _uow.SaveChangesAsync();
                    
                    _logger.LogInformation("PDF generated and uploaded to Cloud Storage for credential {CredentialId}", credential.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate/upload PDF for credential {CredentialId}, will retry later", credential.Id);
                    // Don't fail the entire operation if PDF generation fails
                }

                return _mapper.Map<CredentialDetailDto>(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credential");
                throw;
            }
        }

        public async Task<CredentialDetailDto> ReviewCredentialAsync(Guid credentialId, ReviewCredentialRequest request, Guid reviewedBy)
        {
            try
            {
                var credential = await _uow.Credentials.GetByIdAsync(credentialId);
                if (credential == null)
                    throw new KeyNotFoundException($"Credential {credentialId} not found");

                if (request.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                {
                    credential.Status = "Issued";
                }
                else if (request.Action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
                {
                    credential.Status = "Revoked";
                }

                credential.ReviewedBy = reviewedBy;
                credential.ReviewedAt = DateTime.UtcNow;
                credential.ReviewNotes = request.ReviewNotes;
                credential.UpdatedAt = DateTime.UtcNow;

                _uow.Credentials.Update(credential);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Reviewed credential {CredentialId}: {Action}", credentialId, request.Action);

                return _mapper.Map<CredentialDetailDto>(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing credential {CredentialId}", credentialId);
                throw;
            }
        }

        public async Task RevokeCredentialAsync(Guid credentialId, RevokeCredentialRequest request, Guid revokedBy)
        {
            try
            {
                var credential = await _uow.Credentials.GetByIdAsync(credentialId);
                if (credential == null)
                    throw new KeyNotFoundException($"Credential {credentialId} not found");

                credential.IsRevoked = true;
                credential.Status = "Revoked";
                credential.RevokedBy = revokedBy;
                credential.RevokedAt = DateTime.UtcNow;
                credential.RevocationReason = request.RevocationReason;
                credential.UpdatedAt = DateTime.UtcNow;

                _uow.Credentials.Update(credential);
                await _uow.SaveChangesAsync();

                try
                {
                    if (credential.BlockchainCredentialId.HasValue)
                    {
                        await _blockchainService.RevokeCredentialOnChainAsync(
                            credential.BlockchainCredentialId.Value
                        );
                    }
                }
                catch (Exception chainEx)
                {
                    _logger.LogError(chainEx, "Error revoking credential on blockchain {CredentialId}", credentialId);
                    // Không throw lại để không chặn API
                }

                _logger.LogInformation("Revoked credential {CredentialId}", credentialId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking credential {CredentialId}", credentialId);
                throw;
            }
        }

        // ==================== STUDENT OPERATIONS ====================

        public async Task<List<CredentialDto>> GetStudentCredentialsAsync(Guid userId, string? certificateType = null)
        {
            try
            {
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                var student = students.FirstOrDefault();

                if (student == null)
                    return new List<CredentialDto>();

                return await GetStudentCredentialsByStudentIdAsync(student.Id, certificateType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student credentials for user {UserId}", userId);
                return new List<CredentialDto>();
            }
        }

        public async Task<List<CredentialDto>> GetStudentCredentialsByStudentIdAsync(Guid studentId, string? certificateType = null)
        {
            try
            {
                var credentials = await _uow.Credentials.FindAsync(c => c.StudentId == studentId);

                if (!string.IsNullOrEmpty(certificateType))
                    credentials = credentials.Where(c => c.CertificateType == certificateType);

                var orderedCredentials = credentials
         .OrderByDescending(c => c.IssuedDate)
        .ToList();

                return _mapper.Map<List<CredentialDto>>(orderedCredentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credentials for student {StudentId}", studentId);
                return new List<CredentialDto>();
            }
        }

        public async Task<StudentCredentialSummaryDto> GetStudentCredentialSummaryAsync(Guid userId)
        {
            try
            {
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                var student = students.FirstOrDefault();

                if (student == null)
                    return new StudentCredentialSummaryDto();

                var credentials = await _uow.Credentials.FindAsync(c => c.StudentId == student.Id);
                var requests = await _uow.CredentialRequests.FindAsync(r => r.StudentId == student.Id);

                return new StudentCredentialSummaryDto
                {
                    TotalCredentials = credentials.Count(),
                    SubjectCompletionCount = credentials.Count(c => c.CertificateType == "SubjectCompletion"),
                    // REMOVED: SemesterCompletionCount - No longer supported
                    RoadmapCompletionCount = credentials.Count(c => c.CertificateType == "RoadmapCompletion"),
                    PendingRequests = requests.Count(r => r.Status == "Pending"),
                    RecentCredentials = _mapper.Map<List<CredentialDto>>(
                     credentials.OrderByDescending(c => c.IssuedDate).Take(5).ToList()
                      ),
                    PendingRequestsList = _mapper.Map<List<CredentialRequestDto>>(
                requests.Where(r => r.Status == "Pending").OrderByDescending(r => r.CreatedAt).Take(5).ToList()
                  )
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential summary for user {UserId}", userId);
                return new StudentCredentialSummaryDto();
            }
        }

        public async Task<Guid?> GetStudentIdByUserIdAsync(Guid userId)
        {
            try
            {
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                return students.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student ID for user {UserId}", userId);
                return null;
            }
        }

        // ==================== CREDENTIAL REQUESTS ====================

        public async Task<PagedResult<CredentialRequestDto>> GetCredentialRequestsAsync(GetCredentialRequestsRequest request)
        {
            try
            {
                var requests = await _uow.CredentialRequests.GetAllAsync();

                if (!string.IsNullOrEmpty(request.Status))
                    requests = requests.Where(r => r.Status == request.Status);

                if (!string.IsNullOrEmpty(request.CertificateType))
                    requests = requests.Where(r => r.CertificateType == request.CertificateType);

                if (request.StudentId.HasValue)
                    requests = requests.Where(r => r.StudentId == request.StudentId.Value);

                if (request.IsAutoGenerated.HasValue)
                    requests = requests.Where(r => r.IsAutoGenerated == request.IsAutoGenerated.Value);

                var totalCount = requests.Count();

                var items = requests
                        .OrderByDescending(r => r.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                           .Take(request.PageSize)
                    .ToList();

                var dtos = _mapper.Map<List<CredentialRequestDto>>(items);

                return new PagedResult<CredentialRequestDto>(dtos, totalCount, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential requests");
                return new PagedResult<CredentialRequestDto>();
            }
        }

        public async Task<CredentialRequestDto?> GetCredentialRequestByIdAsync(Guid id)
        {
            try
            {
                var request = await _uow.CredentialRequests.GetByIdAsync(id);
                if (request == null) return null;

                return _mapper.Map<CredentialRequestDto>(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential request {RequestId}", id);
                return null;
            }
        }

        public async Task<CredentialRequestDto> RequestCredentialAsync(Guid userId, RequestCredentialRequest request)
        {
            try
            {
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                var student = students.FirstOrDefault();

                if (student == null)
                    throw new InvalidOperationException("Student not found for current user");

                // Validate request type
                await ValidateCertificateRequestAsync(student.Id, request);

                // Check existing request
                var existingRequests = await _uow.CredentialRequests.FindAsync(r =>
          r.StudentId == student.Id &&
            r.CertificateType == request.CertificateType &&
                r.Status == "Pending");

                if (request.SubjectId.HasValue)
                    existingRequests = existingRequests.Where(r => r.SubjectId == request.SubjectId.Value);
                if (request.SemesterId.HasValue)
                    existingRequests = existingRequests.Where(r => r.SemesterId == request.SemesterId.Value);
                if (request.RoadmapId.HasValue)
                    existingRequests = existingRequests.Where(r => r.StudentRoadmapId == request.RoadmapId.Value);

                if (existingRequests.Any())
                    throw new InvalidOperationException("A pending request already exists for this certificate");

                var credentialRequest = new CredentialRequest
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    CertificateType = request.CertificateType,
                    SubjectId = request.SubjectId,
                    SemesterId = request.SemesterId,
                    StudentRoadmapId = request.RoadmapId,
                    Status = "Pending",
                    IsAutoGenerated = false,
                    StudentNotes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.CredentialRequests.AddAsync(credentialRequest);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} requested certificate: {Type}",
            student.Id, request.CertificateType);

                // Reload entity with navigation properties for proper DTO mapping
                var savedRequest = await _uow.CredentialRequests.GetByIdAsync(credentialRequest.Id);
                return _mapper.Map<CredentialRequestDto>(savedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credential request");
                throw;
            }
        }

        public async Task<List<CredentialRequestDto>> GetStudentCredentialRequestsAsync(Guid userId, string? status = null)
        {
            try
            {
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                var student = students.FirstOrDefault();

                if (student == null)
                    return new List<CredentialRequestDto>();

                var requests = await _uow.CredentialRequests.FindAsync(r => r.StudentId == student.Id);

                if (!string.IsNullOrEmpty(status))
                    requests = requests.Where(r => r.Status == status);

                var orderedRequests = requests
                        .OrderByDescending(r => r.CreatedAt)
              .ToList();

                return _mapper.Map<List<CredentialRequestDto>>(orderedRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student credential requests for user {UserId}", userId);
                return new List<CredentialRequestDto>();
            }
        }

        public async Task<CredentialDetailDto?> ProcessCredentialRequestAsync(
            Guid requestId,
            ProcessCredentialRequestRequest request,
            Guid processedBy)
        {
            try
            {
                var credentialRequest = await _uow.CredentialRequests.GetByIdAsync(requestId);
                if (credentialRequest == null)
                    throw new KeyNotFoundException($"Credential request {requestId} not found");

                if (credentialRequest.Status != "Pending")
                    throw new InvalidOperationException($"Request is already {credentialRequest.Status}");

                if (request.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                {
                    // Determine template (kept for future use if IssueCredentialAsync supports templates)
                    var templateId = request.TemplateId;
                    if (!templateId.HasValue)
                    {
                        var templates = await _uow.CertificateTemplates.FindAsync(t =>
                            t.TemplateType == credentialRequest.CertificateType &&
                            t.IsDefault &&
                            t.IsActive);

                        var defaultTemplate = templates.FirstOrDefault();
                        if (defaultTemplate == null)
                            throw new InvalidOperationException("No default template found for this certificate type");

                        templateId = defaultTemplate.Id;
                    }

                    // Use blockchain-enabled issuing flow
                    var issueDto = new IssueCredentialDto
                    {
                        StudentId = credentialRequest.StudentId,
                        Type = credentialRequest.CertificateType,
                        SubjectId = credentialRequest.SubjectId,
                        StudentRoadmapId = credentialRequest.StudentRoadmapId
                    };

                    var issueResult = await IssueCredentialAsync(issueDto);

                    if (!issueResult.Success || issueResult.Data == null)
                        throw new InvalidOperationException(issueResult.Message ?? "Failed to issue credential");

                    var credentialDto = issueResult.Data;

                    // Update request
                    credentialRequest.Status = "Approved";
                    credentialRequest.ProcessedBy = processedBy;
                    credentialRequest.ProcessedAt = DateTime.UtcNow;
                    credentialRequest.AdminNotes = request.AdminNotes;
                    credentialRequest.CredentialId = credentialDto.Id;
                    credentialRequest.UpdatedAt = DateTime.UtcNow;

                    _uow.CredentialRequests.Update(credentialRequest);
                    await _uow.SaveChangesAsync();

                    _logger.LogInformation(
                        "Approved credential request {RequestId}, issued credential {CredentialId}",
                        requestId,
                        credentialDto.Id);

                    return credentialDto;
                }
                else if (request.Action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
                {
                    credentialRequest.Status = "Rejected";
                    credentialRequest.ProcessedBy = processedBy;
                    credentialRequest.ProcessedAt = DateTime.UtcNow;
                    credentialRequest.AdminNotes = request.AdminNotes;
                    credentialRequest.UpdatedAt = DateTime.UtcNow;

                    _uow.CredentialRequests.Update(credentialRequest);
                    await _uow.SaveChangesAsync();

                    _logger.LogInformation("Rejected credential request {RequestId}", requestId);

                    return null;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid action: {request.Action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credential request {RequestId}", requestId);
                throw;
            }
        }

        // ==================== TEMPLATE MANAGEMENT ====================

        public async Task<List<CertificateTemplateDto>> GetTemplatesAsync(string? templateType = null, bool includeInactive = false)
        {
            try
            {
                var templates = await _uow.CertificateTemplates.GetAllAsync();

                if (!string.IsNullOrEmpty(templateType))
                    templates = templates.Where(t => t.TemplateType == templateType);

                if (!includeInactive)
                    templates = templates.Where(t => t.IsActive);

                var orderedTemplates = templates
        .OrderBy(t => t.TemplateType)
      .ThenByDescending(t => t.IsDefault)
         .ThenBy(t => t.Name)
       .ToList();

                return _mapper.Map<List<CertificateTemplateDto>>(orderedTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates");
                return new List<CertificateTemplateDto>();
            }
        }

        public async Task<List<CertificateTemplateDto>> GetSampleTemplatesAsync(string? templateType = null)
        {
            try
            {
                var templates = await _uow.CertificateTemplates.FindAsync(t => t.IsSample && t.IsActive);

                if (!string.IsNullOrEmpty(templateType))
                    templates = templates.Where(t => t.TemplateType == templateType);

                var orderedTemplates = templates
    .OrderBy(t => t.TemplateType)
 .ThenBy(t => t.Name)
          .ToList();

                return _mapper.Map<List<CertificateTemplateDto>>(orderedTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sample templates");
                return new List<CertificateTemplateDto>();
            }
        }

        public async Task<CertificateTemplateDto?> GetTemplateByIdAsync(Guid id)
        {
            try
            {
                var template = await _uow.CertificateTemplates.GetByIdAsync(id);
                if (template == null) return null;

                return _mapper.Map<CertificateTemplateDto>(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId}", id);
                return null;
            }
        }

        public async Task<CertificateTemplateDto> CreateTemplateAsync(CreateCertificateTemplateRequest request)
        {
            try
            {
                if (request.IsDefault)
                {
                    var existingDefaults = await _uow.CertificateTemplates.FindAsync(t =>
                          t.TemplateType == request.TemplateType &&
                  t.IsDefault);

                    foreach (var existing in existingDefaults)
                    {
                        existing.IsDefault = false;
                        _uow.CertificateTemplates.Update(existing);
                    }
                }

                var template = new CertificateTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    TemplateType = request.TemplateType,
                    Description = request.Description,
                    TemplateContent = request.TemplateContent,
                    TemplateFileUrl = request.TemplateFileUrl,
                    HeaderImagePath = request.HeaderImagePath,
                    FooterImagePath = request.FooterImagePath,
                    BackgroundImagePath = request.BackgroundImagePath,
                    LogoImagePath = request.LogoImagePath,
                    SignatureImagePath = request.SignatureImagePath,
                    TemplateVariables = request.TemplateVariables,
                    CustomStyles = request.CustomStyles,
                    PageSize = request.PageSize,
                    Orientation = request.Orientation,
                    IsDefault = request.IsDefault,
                    IsActive = true,
                    IsSample = false,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.CertificateTemplates.AddAsync(template);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Created template {TemplateId}: {Name}", template.Id, template.Name);

                return _mapper.Map<CertificateTemplateDto>(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                throw;
            }
        }

        public async Task<CertificateTemplateDto> UpdateTemplateAsync(Guid id, UpdateCertificateTemplateRequest request)
        {
            try
            {
                var template = await _uow.CertificateTemplates.GetByIdAsync(id);
                if (template == null)
                    throw new KeyNotFoundException($"Template {id} not found");

                if (!string.IsNullOrEmpty(request.Name))
                    template.Name = request.Name;

                if (request.Description != null)
                    template.Description = request.Description;

                if (request.TemplateContent != null)
                    template.TemplateContent = request.TemplateContent;

                if (request.TemplateFileUrl != null)
                    template.TemplateFileUrl = request.TemplateFileUrl;

                if (request.HeaderImagePath != null)
                    template.HeaderImagePath = request.HeaderImagePath;

                if (request.FooterImagePath != null)
                    template.FooterImagePath = request.FooterImagePath;

                if (request.BackgroundImagePath != null)
                    template.BackgroundImagePath = request.BackgroundImagePath;

                if (request.LogoImagePath != null)
                    template.LogoImagePath = request.LogoImagePath;

                if (request.SignatureImagePath != null)
                    template.SignatureImagePath = request.SignatureImagePath;

                if (request.TemplateVariables != null)
                    template.TemplateVariables = request.TemplateVariables;

                if (request.CustomStyles != null)
                    template.CustomStyles = request.CustomStyles;

                if (request.PageSize != null)
                    template.PageSize = request.PageSize;

                if (request.Orientation != null)
                    template.Orientation = request.Orientation;

                if (request.IsActive.HasValue)
                    template.IsActive = request.IsActive.Value;

                if (request.IsDefault.HasValue && request.IsDefault.Value)
                {
                    var existingDefaults = await _uow.CertificateTemplates.FindAsync(t =>
              t.TemplateType == template.TemplateType &&
                    t.IsDefault &&
                       t.Id != id);

                    foreach (var existing in existingDefaults)
                    {
                        existing.IsDefault = false;
                        _uow.CertificateTemplates.Update(existing);
                    }

                    template.IsDefault = true;
                }

                template.Version++;
                template.UpdatedAt = DateTime.UtcNow;

                _uow.CertificateTemplates.Update(template);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Updated template {TemplateId}", id);

                return _mapper.Map<CertificateTemplateDto>(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", id);
                throw;
            }
        }

        public async Task DeleteTemplateAsync(Guid id)
        {
            try
            {
                var template = await _uow.CertificateTemplates.GetByIdAsync(id);
                if (template == null)
                    throw new KeyNotFoundException($"Template {id} not found");

                var credentialsUsingTemplate = await _uow.Credentials.FindAsync(c => c.CertificateTemplateId == id);
                if (credentialsUsingTemplate.Any())
                    throw new InvalidOperationException("Cannot delete template that is in use by credentials");

                _uow.CertificateTemplates.Delete(template);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Deleted template {TemplateId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                throw;
            }
        }

        // ==================== VERIFICATION & SHARING ====================

        public async Task<CredentialVerificationDto> VerifyCredentialAsync(VerifyCredentialRequest request)
        {
            try
            {
                Credential? credential = null;

                if (!string.IsNullOrEmpty(request.CredentialNumber))
                {
                    var credentials = await _uow.Credentials.FindAsync(c => c.CredentialId == request.CredentialNumber);
                    credential = credentials.FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(request.VerificationHash))
                {
                    var credentials = await _uow.Credentials.FindAsync(c => c.VerificationHash == request.VerificationHash);
                    credential = credentials.FirstOrDefault();
                }

                if (credential == null)
                {
                    return new CredentialVerificationDto
                    {
                        IsValid = false,
                        Message = "Certificate not found",
                        VerifiedAt = DateTime.UtcNow
                    };
                }

                if (credential.IsRevoked || credential.Status == "Revoked")
                {
                    return new CredentialVerificationDto
                    {
                        IsValid = false,
                        Message = $"Certificate has been revoked. Reason: {credential.RevocationReason}",
                        Credential = _mapper.Map<CredentialDto>(credential),
                        VerifiedAt = DateTime.UtcNow
                    };
                }

                bool? onChainValid = null;
                try
                {
                    if (credential.BlockchainCredentialId.HasValue)
                    {
                        onChainValid = await _blockchainService.VerifyCredentialOnChainAsync(
                            credential.BlockchainCredentialId.Value
                        );
                    }
                }
                catch (Exception chainEx)
                {
                    _logger.LogWarning(chainEx, "Error verifying credential on-chain for {CredentialId}", credential.Id);
                }

                var isValid = !onChainValid.HasValue || onChainValid.Value;

                return new CredentialVerificationDto
                {
                    IsValid = isValid,
                    Message = isValid
                        ? "Certificate is valid and authentic"
                        : "On-chain verification failed",
                    Credential = _mapper.Map<CredentialDto>(credential),
                    VerifiedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying credential");
                return new CredentialVerificationDto
                {
                    IsValid = false,
                    Message = "Error verifying certificate",
                    VerifiedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<CredentialShareDto> GetCredentialShareInfoAsync(Guid credentialId, Guid? userId)
        {
            try
            {
                var credential = await _uow.Credentials.GetByIdAsync(credentialId);
                if (credential == null)
                    throw new KeyNotFoundException("Credential not found");

                if (userId.HasValue)
                {
                    var student = await _uow.Students.GetByIdAsync(credential.StudentId);
                    if (student?.UserId != userId.Value)
                    {
                        throw new UnauthorizedAccessException("You don't have permission to access this credential");
                    }
                }

                var canShare = credential.Status == "Issued" && !credential.IsRevoked;

                var baseUrl = _frontendSettings.BaseUrl.TrimEnd('/');
                var verifyPath = _frontendSettings.VerifyPath.TrimStart('/').TrimEnd('/');
                var shareableUrl = $"{baseUrl}/{verifyPath}/{credential.CredentialId}";

                if (string.IsNullOrEmpty(credential.ShareableUrl))
                {
                    credential.ShareableUrl = shareableUrl;
                    _uow.Credentials.Update(credential);
                    await _uow.SaveChangesAsync();
                }

                return new CredentialShareDto
                {
                    CredentialId = credential.Id,
                    CredentialNumber = credential.CredentialId,
                    StudentName = credential.Student?.User?.FullName ?? "Unknown",
                    CertificateType = credential.CertificateType,
                    SubjectName = credential.Subject?.SubjectName,
                    IssuedDate = credential.IssuedDate,
                    QRCodeData = credential.QRCodeData,
                    ShareableUrl = credential.ShareableUrl ?? shareableUrl,
                    VerificationHash = credential.VerificationHash ?? "",
                    CanShare = canShare
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting share info for credential {CredentialId}", credentialId);
                throw;
            }
        }

        // ==================== PDF & QR CODE ====================

        public async Task<(byte[] FileBytes, string FileName)> GenerateCredentialPdfAsync(Guid credentialId)
        {
            try
            {
                _logger.LogInformation("Generating PDF for credential {CredentialId}", credentialId);

                // Get credential with all related data
                var credential = await _uow.Credentials.GetByIdAsync(credentialId);
                
                if (credential == null)
                    throw new KeyNotFoundException($"Credential {credentialId} not found");

                if (credential.IsRevoked)
                    throw new InvalidOperationException("Cannot generate PDF for revoked credential");

                // Generate PDF using PdfService
                var pdfBytes = await _pdfService.GenerateCertificatePdfAsync(credential);
                
                // Generate filename
                var fileName = $"{credential.CredentialId}.pdf";

                // Upload to Cloud Storage
                try
                {
                    var cloudUrl = await _cloudStorageService.UploadPdfAsync(pdfBytes, fileName);
                    
                    // Update credential with Cloud URL
                    credential.PdfUrl = cloudUrl;
                    credential.PdfFilePath = $"cloudinary://{fileName}"; // Mark as Cloudinary storage
                    credential.UpdatedAt = DateTime.UtcNow;
                    
                    _uow.Credentials.Update(credential);
                    await _uow.SaveChangesAsync();
                    
                    _logger.LogInformation("PDF uploaded to Cloud Storage for credential {CredentialId}, URL: {Url}", 
                        credentialId, cloudUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload PDF to Firebase for credential {CredentialId}, continuing with local generation", 
                        credentialId);
                    // Continue without Firebase upload - return bytes anyway
                }

                return (pdfBytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for credential {CredentialId}", credentialId);
                throw;
            }
        }

        public async Task<string> GenerateQRCodeAsync(Guid credentialId, Guid? userId, int size = 300)
        {
          try
            {
          _logger.LogInformation("Generating QR code for credential {CredentialId}", credentialId);

       // 1. Get credential
     var credential = await _uow.Credentials.GetByIdAsync(credentialId);
      if (credential == null)
          throw new KeyNotFoundException($"Credential {credentialId} not found");

        // 2. Check permission if userId is provided
    if (userId.HasValue)
  {
           var student = await _uow.Students.GetByIdAsync(credential.StudentId);
         if (student?.UserId != userId.Value)
       {
    // Check if user is admin (would need to pass role info)
  _logger.LogWarning("User {UserId} attempted to access credential {CredentialId} without permission", 
            userId, credentialId);
    throw new UnauthorizedAccessException("You don't have permission to access this credential");
 }
      }

        // 3. Generate verification URL
        var baseUrl = _frontendSettings.BaseUrl.TrimEnd('/');
        var verifyPath = _frontendSettings.VerifyPath.TrimStart('/').TrimEnd('/');
        var verifyUrl = $"{baseUrl}/{verifyPath}/{credential.CredentialId}";

        // 4. Generate QR code using PdfService
        var base64QRCode = _pdfService.GenerateQRCode(verifyUrl, size / 20);

        // 5. Save to credential if not already saved
        if (string.IsNullOrEmpty(credential.QRCodeData))
        {
            credential.QRCodeData = base64QRCode;
            credential.ShareableUrl = verifyUrl;
            _uow.Credentials.Update(credential);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Saved QR code to credential {CredentialId}", credentialId);
        }

        return base64QRCode;
            }
            catch (KeyNotFoundException)
    {
        throw;
   }
    catch (UnauthorizedAccessException)
        {
       throw;
            }
       catch (Exception ex)
   {
     _logger.LogError(ex, "Error generating QR code for credential {CredentialId}", credentialId);
 throw new InvalidOperationException("Failed to generate QR code", ex);
     }
        }

        public Task<(byte[] FileBytes, string FileName)> PreviewTemplateAsync(Guid templateId)
 {
            // TODO: Use same PDF generation as credentials with sample data
    throw new NotImplementedException("Template preview uses PDF generation - implement GenerateCredentialPdfAsync first");
        }

        // ==================== HELPER METHODS ====================

        private async Task<string> GenerateCredentialNumberAsync(string certificateType)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = certificateType switch
            {
                "SubjectCompletion" => "SUB",
                "SemesterCompletion" => "SEM",
                "RoadmapCompletion" => "GRAD",
                _ => "CERT"
            };

            var count = await _uow.Credentials.CountAsync(c => c.CertificateType == certificateType);
            var sequence = (count + 1).ToString("D6");

            return $"{prefix}-{year}-{sequence}";
        }

        private string GenerateVerificationHash(string credentialNumber, Guid studentId)
        {
            var data = $"{credentialNumber}|{studentId}|{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task ValidateCertificateRequestAsync(Guid studentId, RequestCredentialRequest request)
        {
            switch (request.CertificateType)
            {
                case "SubjectCompletion":
                    if (!request.SubjectId.HasValue)
                        throw new InvalidOperationException("SubjectId is required for Subject Completion certificate");

                    var grades = await _uow.Grades.FindAsync(g =>
                      g.StudentId == studentId &&
                      g.SubjectId == request.SubjectId.Value);

                    var hasPassed = grades.Any(g => g.Score >= 5.0m);
                    if (!hasPassed)
                        throw new InvalidOperationException("Student has not passed this subject");
                    break;

                case "SemesterCompletion":
                    if (!request.SemesterId.HasValue)
                        throw new InvalidOperationException("SemesterId is required for Semester Completion certificate");
                    break;

                case "RoadmapCompletion":
                    if (!request.RoadmapId.HasValue)
                        throw new InvalidOperationException("RoadmapId is required for Roadmap Completion certificate");
                    break;

                default:
                    throw new InvalidOperationException($"Invalid certificate type: {request.CertificateType}");
            }
        }

        // ==================== HELPER METHODS (NEW) ====================

        /// <summary>
        /// Tạo URL xác thực công khai cho chứng chỉ
        /// </summary>
        private string BuildVerificationUrl(Credential credential)
        {
            var baseUrl = _frontendSettings.BaseUrl.TrimEnd('/');
            var verifyPath = _frontendSettings.VerifyPath.Trim('/');

            var credentialNumber = credential.CredentialId; // Mã chứng chỉ dạng SUB-YYYY-XXXXXX
            var verificationHash = credential.VerificationHash ?? string.Empty;

            // URL dạng:
            // {BaseUrl}/{VerifyPath}/{credentialNumber}?credentialNumber=...&verificationHash=...
            var url = $"{baseUrl}/{verifyPath}/{Uri.EscapeDataString(credentialNumber)}";

            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(credentialNumber))
            {
                queryParts.Add($"credentialNumber={Uri.EscapeDataString(credentialNumber)}");
            }
            if (!string.IsNullOrWhiteSpace(verificationHash))
            {
                queryParts.Add($"verificationHash={Uri.EscapeDataString(verificationHash)}");
            }

            if (queryParts.Count > 0)
            {
                url += "?" + string.Join("&", queryParts);
            }

            return url;
        }

        /// <summary>
        /// Tạo QR Code từ URL
        /// </summary>
        private string GenerateQrCodeImage(string url, int size)
        {
            return _pdfService.GenerateQRCode(url, size / 20);
        }

        /// <summary>
        /// Đảm bảo chứng chỉ đã có ShareableUrl và QRCode, nếu chưa thì tạo mới
        /// </summary>
        private async Task<(string url, string qrCode)> EnsureShareArtifactsAsync(Credential credential)
        {
            var needsUpdate = false;

            if (string.IsNullOrWhiteSpace(credential.ShareableUrl))
            {
                credential.ShareableUrl = BuildVerificationUrl(credential);
                needsUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(credential.QRCodeData))
            {
                credential.QRCodeData = GenerateQrCodeImage(
                    credential.ShareableUrl, 
                    _frontendSettings.DefaultQrSize);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                _uow.Credentials.Update(credential);
                await _uow.SaveChangesAsync();
            }

            return (credential.ShareableUrl, credential.QRCodeData);
        }

        // ==================== PUBLIC VIEW METHODS (NEW) ====================

        /// <summary>
        /// Lấy thông tin chứng chỉ công khai - Dành cho người xem qua QR/Link (AllowAnonymous)
        /// </summary>
        public async Task<CertificatePublicDto?> GetPublicCertificateAsync(Guid credentialId)
        {
            try
            {
                var credential = await _uow.Credentials.GetByIdAsync(credentialId);
                if (credential == null)
                    return null;

                // Đảm bảo có URL và QR Code
                var (publicUrl, qrCode) = await EnsureShareArtifactsAsync(credential);

                // Map sang DTO
                var dto = _mapper.Map<CertificatePublicDto>(credential);
                dto.PublicUrl = publicUrl;
                dto.QrCodeData = qrCode;

                // Xác định trạng thái xác thực
                if (credential.IsRevoked)
                {
                    dto.VerificationStatus = "Revoked";
                    dto.VerificationMessage = $"Certificate has been revoked on {credential.RevokedAt?.ToString("yyyy-MM-dd")}";
                }
                else if (credential.IsOnBlockchain && credential.BlockchainCredentialId.HasValue)
                {
                    // Verify on blockchain bằng credentialId on-chain
                    var isValid = await _blockchainService.VerifyCredentialOnChainAsync(
                        credential.BlockchainCredentialId.Value
                    );

                    dto.VerificationStatus = isValid ? "Verified" : "Pending";
                    dto.VerificationMessage = isValid
                        ? "Certificate is verified on blockchain"
                        : "Certificate verification pending";
                }
                else
                {
                    dto.VerificationStatus = "Pending";
                    dto.VerificationMessage = "Certificate not yet recorded on blockchain";
                }

                // Increment view count
                credential.ViewCount++;
                credential.LastViewedAt = DateTime.UtcNow;
                _uow.Credentials.Update(credential);
                await _uow.SaveChangesAsync();

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public certificate {CredentialId}", credentialId);
                return null;
            }
        }

        /// <summary>
        /// Lấy thông tin chứng chỉ công khai theo CredentialId (SUB-YYYY-XXXXXX)
        /// Dùng cho endpoint /api/credentials/public/{credentialNumber}
        /// </summary>
        public async Task<CertificatePublicDto?> GetPublicCertificateByNumberAsync(string credentialNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(credentialNumber))
                {
                    return null;
                }

                var credential = (await _uow.Credentials.FindAsync(c => c.CredentialId == credentialNumber))
                    .FirstOrDefault();

                if (credential == null)
                {
                    return null;
                }

                return await GetPublicCertificateAsync(credential.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public certificate by number {CredentialNumber}", credentialNumber);
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách chứng chỉ của sinh viên hiện tại để chia sẻ (kèm QR + URL)
        /// </summary>
        public async Task<List<CertificatePublicDto>> GetMyCertificatesForSharingAsync(Guid userId)
        {
            try
            {
                // Tìm student từ userId
                var students = await _uow.Students.FindAsync(s => s.UserId == userId);
                var student = students.FirstOrDefault();
                
                if (student == null)
                {
                    _logger.LogWarning("Student not found for userId {UserId}", userId);
                    return new List<CertificatePublicDto>();
                }

                // Lấy tất cả chứng chỉ chưa bị thu hồi
                var credentials = await _uow.Credentials.FindAsync(c => 
                    c.StudentId == student.Id && 
                    !c.IsRevoked &&
                    c.Status == "Issued");

                var result = new List<CertificatePublicDto>();

                foreach (var credential in credentials)
                {
                    var dto = await GetPublicCertificateAsync(credential.Id);
                    if (dto != null)
                    {
                        result.Add(dto);
                    }
                }

                return result.OrderByDescending(c => c.IssuedDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shareable certificates for user {UserId}", userId);
                return new List<CertificatePublicDto>();
            }
        }
    }
}
