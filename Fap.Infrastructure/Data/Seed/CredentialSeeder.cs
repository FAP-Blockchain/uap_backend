using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Credentials, CredentialRequests, and CertificateTemplates with comprehensive test scenarios
    /// </summary>
    public class CredentialSeeder : BaseSeeder
    {
        public CredentialSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            // Seed templates first
            await SeedCertificateTemplatesAsync();

            // Then seed credential requests
            await SeedCredentialRequestsAsync();

            // Finally seed credentials
            await SeedCredentialsAsync();
        }

        private async Task SeedCertificateTemplatesAsync()
        {
            if (await _context.CertificateTemplates.AnyAsync())
            {
                Console.WriteLine("⏭️ Certificate Templates already exist. Skipping...");
                return;
            }

            var templates = new List<CertificateTemplate>
            {
                // Subject Completion Template (Default)
                new CertificateTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Subject Completion Certificate",
                    TemplateType = "SubjectCompletion",
                    Description = "Standard certificate for completing a subject",
                    TemplateContent = "<html><body><h1>Certificate of Completion</h1><p>{{StudentName}} has successfully completed {{SubjectName}}</p></body></html>",
                    PageSize = "A4",
                    Orientation = "Landscape",
                    IsDefault = true,
                    IsActive = true,
                    IsSample = false,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow
                },

                // Semester Completion Template (Default)
                new CertificateTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Semester Completion Certificate",
                    TemplateType = "SemesterCompletion",
                    Description = "Certificate for completing a semester",
                    TemplateContent = "<html><body><h1>Semester Completion</h1><p>{{StudentName}} completed {{SemesterName}} with GPA {{FinalGrade}}</p></body></html>",
                    PageSize = "A4",
                    Orientation = "Landscape",
                    IsDefault = true,
                    IsActive = true,
                    IsSample = false,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow
                },

                // Roadmap Completion Template (Default)
                new CertificateTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Graduation Certificate",
                    TemplateType = "RoadmapCompletion",
                    Description = "Graduation certificate for completing entire program",
                    TemplateContent = "<html><body><h1>Certificate of Graduation</h1><p>{{StudentName}} has graduated with {{Classification}}</p></body></html>",
                    PageSize = "A4",
                    Orientation = "Landscape",
                    IsDefault = true,
                    IsActive = true,
                    IsSample = false,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow
                },

                // Sample Template
                new CertificateTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Sample Excellence Certificate",
                    TemplateType = "SubjectCompletion",
                    Description = "Sample template for demonstration",
                    TemplateContent = "<html><body><h1>Excellence Award</h1></body></html>",
                    PageSize = "A4",
                    Orientation = "Portrait",
                    IsDefault = false,
                    IsActive = true,
                    IsSample = true,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.CertificateTemplates.AddRangeAsync(templates);
            await SaveAsync("CertificateTemplates");

            Console.WriteLine($"   ✅ Created {templates.Count} certificate templates");
        }

        private async Task SeedCredentialRequestsAsync()
        {
            if (await _context.CredentialRequests.AnyAsync())
            {
                Console.WriteLine("⏭️ Credential Requests already exist. Skipping...");
                return;
            }

            var students = await _context.Students.Take(5).ToListAsync();
            var subjects = await _context.Subjects.Take(3).ToListAsync();
            var semesters = await _context.Semesters.Take(2).ToListAsync();

            if (!students.Any() || !subjects.Any())
            {
                Console.WriteLine("⚠️ No students or subjects found. Skipping credential requests...");
                return;
            }

            var requests = new List<CredentialRequest>();

            // Test Case 1: Pending auto-generated subject completion request
            requests.Add(new CredentialRequest
            {
                Id = Guid.NewGuid(),
                StudentId = students[0].Id,
                CertificateType = "SubjectCompletion",
                SubjectId = subjects[0].Id,
                Status = "Pending",
                FinalGrade = 8.5m,
                LetterGrade = "B+",
                CompletionDate = DateTime.UtcNow.AddDays(-5),
                IsAutoGenerated = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });

            // Test Case 2: Pending manual request
            requests.Add(new CredentialRequest
            {
                Id = Guid.NewGuid(),
                StudentId = students[1].Id,
                CertificateType = "SubjectCompletion",
                SubjectId = subjects[1].Id,
                Status = "Pending",
                FinalGrade = 7.0m,
                LetterGrade = "B",
                CompletionDate = DateTime.UtcNow.AddDays(-3),
                IsAutoGenerated = false,
                StudentNotes = "Please issue my certificate, I need it for scholarship application",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            });

            // Test Case 3: Approved request (will have credential)
            var approvedRequestId = Guid.NewGuid();
            requests.Add(new CredentialRequest
            {
                Id = approvedRequestId,
                StudentId = students[2].Id,
                CertificateType = "SubjectCompletion",
                SubjectId = subjects[2].Id,
                Status = "Approved",
                FinalGrade = 9.0m,
                LetterGrade = "A",
                CompletionDate = DateTime.UtcNow.AddDays(-10),
                IsAutoGenerated = true,
                ProcessedAt = DateTime.UtcNow.AddDays(-8),
                AdminNotes = "Approved - excellent performance",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });

            // Test Case 4: Rejected request
            requests.Add(new CredentialRequest
            {
                Id = Guid.NewGuid(),
                StudentId = students[3].Id,
                CertificateType = "SubjectCompletion",
                SubjectId = subjects[0].Id,
                Status = "Rejected",
                FinalGrade = 4.5m,
                LetterGrade = "D",
                CompletionDate = DateTime.UtcNow.AddDays(-15),
                IsAutoGenerated = false,
                ProcessedAt = DateTime.UtcNow.AddDays(-14),
                AdminNotes = "Rejected - grade below passing threshold",
                StudentNotes = "I believe I passed this subject",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            });

            // Test Case 5: Semester completion request (pending)
            if (semesters.Any())
            {
                requests.Add(new CredentialRequest
                {
                    Id = Guid.NewGuid(),
                    StudentId = students[4].Id,
                    CertificateType = "SemesterCompletion",
                    SemesterId = semesters[0].Id,
                    Status = "Pending",
                    FinalGrade = 7.8m,
                    Classification = "Good",
                    CompletionDate = DateTime.UtcNow.AddDays(-2),
                    IsAutoGenerated = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }

            await _context.CredentialRequests.AddRangeAsync(requests);
            await SaveAsync("CredentialRequests");

            Console.WriteLine($"   ✅ Created {requests.Count} credential requests:");
            Console.WriteLine($"      • Pending: {requests.Count(r => r.Status == "Pending")}");
            Console.WriteLine($"      • Approved: {requests.Count(r => r.Status == "Approved")}");
            Console.WriteLine($"      • Rejected: {requests.Count(r => r.Status == "Rejected")}");
        }

        private async Task SeedCredentialsAsync()
        {
            if (await _context.Credentials.AnyAsync())
            {
                Console.WriteLine("⏭️ Credentials already exist. Skipping...");
                return;
            }

            var students = await _context.Students.Take(5).ToListAsync();
            var subjects = await _context.Subjects.Take(3).ToListAsync();
            var templates = await _context.CertificateTemplates
                .Where(t => !t.IsSample)
                .ToListAsync();

            if (!students.Any() || !templates.Any())
            {
                Console.WriteLine("⚠️ No students or templates found. Skipping credentials...");
                return;
            }

            var credentials = new List<Credential>();
            var counter = 1;

            // ====== TEST CASE 1: Issued with Blockchain + QR Code (Fully Complete) ======
            var cred1 = CreateCredential(
                students[0].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[0].Id,
                finalGrade: 8.5m,
                letterGrade: "B+",
                status: "Issued",
                withQRCode: true,
                viewCount: 25,
                blockchainCredentialId: 1
            );
            credentials.Add(cred1);

            // ====== TEST CASE 2: Issued WITHOUT QR Code (Lazy Generation Test) ======
            var cred2 = CreateCredential(
                students[1].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[1].Id,
                finalGrade: 9.0m,
                letterGrade: "A",
                status: "Issued",
                withQRCode: false,  // QR will be generated on first public view
                viewCount: 0,
                blockchainCredentialId: 2
            );
            credentials.Add(cred2);

            // ====== TEST CASE 3: Revoked Certificate (Cannot be Shared) ======
            var cred3 = CreateCredential(
                students[2].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[2].Id,
                finalGrade: 6.0m,
                letterGrade: "C",
                status: "Revoked",
                isRevoked: true,
                revocationReason: "Certificate issued in error - student did not meet attendance requirements",
                withQRCode: false,  // Revoked certificates lose QR code
                viewCount: 5,  // Had some views before revocation
                blockchainCredentialId: 3  // Still has blockchain ID even when revoked
            );
            credentials.Add(cred3);

            // ====== TEST CASE 4: Semester Completion - Issued with QR ======
            var semesters = await _context.Semesters.Take(1).ToListAsync();
            if (semesters.Any() && templates.Any(t => t.TemplateType == "SemesterCompletion"))
            {
                var cred4 = CreateCredential(
                    students[3].Id,
                    templates.First(t => t.TemplateType == "SemesterCompletion").Id,
                    "SemesterCompletion",
                    counter++,
                    semesterId: semesters[0].Id,
                    finalGrade: 7.8m,
                    classification: "Good",
                    status: "Issued",
                    withQRCode: true,
                    viewCount: 12,
                    blockchainCredentialId: 4
                );
                credentials.Add(cred4);
            }

            // ====== TEST CASE 5: High View Count (Popular Certificate) ======
            var cred5 = CreateCredential(
                students[4].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[0].Id,
                finalGrade: 9.5m,
                letterGrade: "A+",
                status: "Issued",
                viewCount: 150,
                withQRCode: true,
                blockchainCredentialId: 5
            );
            credentials.Add(cred5);

            // ====== TEST CASE 6: Graduation Certificate (Roadmap) ======
            var roadmaps = await _context.StudentRoadmaps.Take(1).ToListAsync();
            if (roadmaps.Any() && templates.Any(t => t.TemplateType == "RoadmapCompletion"))
            {
                var cred6 = CreateCredential(
                    students[0].Id,
                    templates.First(t => t.TemplateType == "RoadmapCompletion").Id,
                    "RoadmapCompletion",
                    counter++,
                    roadmapId: roadmaps[0].Id,
                    finalGrade: 8.2m,
                    classification: "Second Class Honours (Upper)",
                    status: "Issued",
                    withQRCode: true,
                    viewCount: 45,
                    blockchainCredentialId: 6
                );
                credentials.Add(cred6);
            }

            // ====== TEST CASE 7: Pending Credential (Not Yet Issued) ======
            var cred7 = CreateCredential(
                students[1].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[1].Id,
                finalGrade: 7.0m,
                letterGrade: "B",
                status: "Pending",
                withQRCode: false,  // No QR for pending
                viewCount: 0
                // No blockchainCredentialId for pending
            );
            credentials.Add(cred7);

            // ====== TEST CASE 8: Recently Issued (No Views Yet) ======
            var cred8 = CreateCredential(
                students[2].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[2].Id,
                finalGrade: 8.0m,
                letterGrade: "B+",
                status: "Issued",
                withQRCode: true,
                viewCount: 0,
                recentlyIssued: true,
                blockchainCredentialId: 7
            );
            credentials.Add(cred8);

            // ====== TEST CASE 9: Issued but Blockchain Pending ======
            var cred9 = CreateCredential(
                students[3].Id,
                templates.First(t => t.TemplateType == "SubjectCompletion").Id,
                "SubjectCompletion",
                counter++,
                subjectId: subjects[0].Id,
                finalGrade: 7.5m,
                letterGrade: "B",
                status: "Issued",
                withQRCode: true,
                blockchainPending: true,  // Blockchain transaction pending
                viewCount: 3
                // No blockchainCredentialId yet - pending
            );
            credentials.Add(cred9);

            await _context.Credentials.AddRangeAsync(credentials);
            await SaveAsync("Credentials");
     
            Console.WriteLine($"   ✅ Created {credentials.Count} credentials:");
            Console.WriteLine($"      • Issued: {credentials.Count(c => c.Status == "Issued")}");
            Console.WriteLine($"      • Pending: {credentials.Count(c => c.Status == "Pending")}");
            Console.WriteLine($"      • Revoked: {credentials.Count(c => c.Status == "Revoked")}");
            Console.WriteLine($"      • With QR Code: {credentials.Count(c => !string.IsNullOrEmpty(c.QRCodeData))}");
            Console.WriteLine($"      • Without QR (Lazy Gen): {credentials.Count(c => c.Status == "Issued" && string.IsNullOrEmpty(c.QRCodeData))}");
            Console.WriteLine($"      • On Blockchain: {credentials.Count(c => c.IsOnBlockchain)}");
            Console.WriteLine($"      • Blockchain Pending: {credentials.Count(c => c.Status == "Issued" && !c.IsOnBlockchain)}");
            Console.WriteLine($"      • With Views: {credentials.Count(c => c.ViewCount > 0)}");
        }

        private Credential CreateCredential(
            Guid studentId,
            Guid templateId,
            string certificateType,
            int sequenceNumber,
            Guid? subjectId = null,
            Guid? semesterId = null,
            Guid? roadmapId = null,
            decimal? finalGrade = null,
            string? letterGrade = null,
            string? classification = null,
            string status = "Issued",
            bool isRevoked = false,
            string? revocationReason = null,
            bool withQRCode = false,
            int viewCount = 0,
            bool recentlyIssued = false,
            bool blockchainPending = false,
            long? blockchainCredentialId = null)
        {
            var year = DateTime.UtcNow.Year;

            var prefix = certificateType switch
            {
                "SubjectCompletion"  => "SUB",
                "SemesterCompletion" => "SEM",
                "RoadmapCompletion"  => "GRAD",
                _                    => "CERT"
            };

            var credentialNumber = $"{prefix}-{year}-{sequenceNumber:D6}";
            var verificationHash = GenerateVerificationHash(credentialNumber, studentId);
            
            // Use frontend URL from settings
            var frontendUrl = "http://localhost:3000";  // This matches appsettings.json
            var shareableUrl = status == "Issued" && !isRevoked
                ? $"{frontendUrl}/certificates/verify/{credentialNumber}"
                : null;  // No shareable URL for pending/revoked

            var random = new Random();
            var daysAgo = recentlyIssued ? random.Next(1, 3) : random.Next(10, 180);

            var credential = new Credential
            {
                Id = Guid.NewGuid(),
                CredentialId = credentialNumber,
                StudentId = studentId,
                CertificateTemplateId = templateId,
                CertificateType = certificateType,
                SubjectId = subjectId,
                SemesterId = semesterId,
                StudentRoadmapId = roadmapId,
                IssuedDate = status == "Issued" 
                    ? DateTime.UtcNow.AddDays(-daysAgo) 
                    : DateTime.UtcNow,  // Set to now for pending, will be updated when issued
                CompletionDate = DateTime.UtcNow.AddDays(-random.Next(15, 200)),  // Always set
                FinalGrade = finalGrade,
                LetterGrade = letterGrade,
                Classification = classification,
                VerificationHash = verificationHash,
                ShareableUrl = shareableUrl,  // Only for issued, non-revoked
                Status = status,
                IsRevoked = isRevoked,
                RevokedAt = isRevoked ? DateTime.UtcNow.AddDays(-5) : null,
                RevocationReason = revocationReason,
                ReviewedAt = status == "Issued"
                    ? DateTime.UtcNow.AddDays(-random.Next(5, 10))
                    : null,
                ViewCount = viewCount,
                LastViewedAt = viewCount > 0
                    ? DateTime.UtcNow.AddHours(-random.Next(1, 48))
                    : null,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(10, 180)),

                // Blockchain data
                IsOnBlockchain = status == "Issued" && !isRevoked && !blockchainPending,
                BlockchainTransactionHash = (status == "Issued" && !blockchainPending)
                    ? $"0x{Guid.NewGuid():N}"
                    : null,
                BlockchainStoredAt = (status == "Issued" && !blockchainPending)
                    ? DateTime.UtcNow.AddDays(-random.Next(5, 10))
                    : null,
                BlockchainCredentialId = blockchainCredentialId,  // Maps to smart contract uint256 ID

                // Required fields
                IPFSHash = status == "Issued" 
                    ? $"Qm{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 46)
                    : "QmPendingHash000000000000000000000000000000",
                
                FileUrl = status == "Issued"
                    ? $"https://ipfs.io/ipfs/Qm{Guid.NewGuid():N}"
                    : "https://ipfs.io/ipfs/QmPending",

                // PDF (placeholder)
                PdfUrl = status == "Issued"
                    ? $"https://api.fap.edu.vn/credentials/{credentialNumber}/download"
                    : null,
                
                // QR Code - only if explicitly requested OR null for lazy generation
                QRCodeData = withQRCode && status == "Issued" && !isRevoked
                    ? GenerateSampleQRCode(shareableUrl!)
                    : null  // Null means lazy generation on first view
            };

            return credential;
        }

        private string GenerateVerificationHash(string credentialNumber, Guid studentId)
        {
            var data = $"{credentialNumber}|{studentId}|{DateTime.UtcNow:yyyyMMdd}";

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        private string GenerateSampleQRCode(string url)
        {
            // This is a placeholder - actual QR code will be generated by the service
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        }
    }
}
