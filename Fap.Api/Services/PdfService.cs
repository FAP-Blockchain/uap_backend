using Fap.Api.Interfaces;
using Fap.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System;

namespace Fap.Api.Services
{
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;

        private static readonly Lazy<TimeZoneInfo> VietnamTimeZone = new(() =>
        {
            // Windows: "SE Asia Standard Time"; Linux: "Asia/Ho_Chi_Minh"
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
                }
            }
        });

        private static DateTime ToVietnamTime(DateTime value)
        {
            // Most persisted timestamps here are UTC. If Kind is Unspecified, treat as UTC.
            var tz = VietnamTimeZone.Value;

            if (value.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(value, tz);
            }

            if (value.Kind == DateTimeKind.Local)
            {
                return TimeZoneInfo.ConvertTime(value, tz);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), tz);
        }

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
            
            // Set QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateCertificatePdfAsync(Credential credential)
        {
            try
            {
                // Create PDF document
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Page settings
                        page.Size(PageSizes.A4.Landscape());
                        // Keep margins modest so content fits on a single page
                        page.Margin(40);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                        // Header
                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("UAP")
                                        .FontSize(16).Bold().FontColor(Colors.Orange.Darken3);
                                    col.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                                        .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);
                                });
                            });

                            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Orange.Medium);
                        });

                        // Content
                        page.Content().PaddingVertical(20).Column(column =>
                        {
                            // Tighten spacing to avoid spilling onto a second page
                            column.Spacing(10);

                            // Certificate Number
                            column.Item().AlignCenter().Text($"Certificate No: {credential.CredentialId}")
                                .FontSize(14).Italic().FontColor(Colors.Grey.Darken2);

                            // Spacing
                            column.Item().PaddingTop(20);

                            // Certificate Text
                            column.Item().AlignCenter().Text("This is to certify that")
                                .FontSize(16);

                            // Student Name - Access through User navigation property
                            column.Item().AlignCenter().Text(credential.Student?.User?.FullName ?? "Unknown Student")
                                .FontSize(26).Bold().FontColor(Colors.Blue.Darken3);

                            // Student Code
                            column.Item().AlignCenter().Text($"({credential.Student?.StudentCode ?? "N/A"})")
                                .FontSize(14).Italic();

                            // Completion Text
                            column.Item().PaddingTop(10).AlignCenter().Text("has successfully completed")
                                .FontSize(16);

                            // Certificate Type Specific Content
                            if (credential.CertificateType == "SubjectCompletion")
                            {
                                column.Item().AlignCenter().Text(credential.Subject?.SubjectName ?? "Unknown Subject")
                                    .FontSize(22).Bold().FontColor(Colors.Orange.Darken2);

                                column.Item().AlignCenter().Text($"Subject Code: {credential.Subject?.SubjectCode ?? "N/A"}")
                                    .FontSize(14);

                                column.Item().PaddingTop(10).AlignCenter().Row(row =>
                                {
                                    row.AutoItem().Text("Grade: ").FontSize(16);
                                    row.AutoItem().Text($"{credential.FinalGrade:F2}")
                                        .FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                                    row.AutoItem().Text($" ({credential.LetterGrade})")
                                        .FontSize(16).Bold();
                                });
                            }
                            else if (credential.CertificateType == "RoadmapCompletion")
                            {
                                column.Item().AlignCenter().Text(credential.Student?.Curriculum?.Name ?? "Program Completion")
                                    .FontSize(22).Bold().FontColor(Colors.Orange.Darken2);

                                column.Item().PaddingTop(10).AlignCenter().Row(row =>
                                {
                                    row.AutoItem().Text("Overall GPA: ").FontSize(16);
                                    row.AutoItem().Text($"{credential.FinalGrade:F2}")
                                        .FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                                });

                                if (!string.IsNullOrEmpty(credential.Classification))
                                {
                                    column.Item().AlignCenter().Text($"Classification: {credential.Classification}")
                                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken1);
                                }
                            }
                            else if (credential.CertificateType == "CurriculumCompletion")
                            {
                                column.Item().AlignCenter().Text(credential.Student?.Curriculum?.Name ?? "University Graduation")
                                    .FontSize(24).Bold().FontColor(Colors.Red.Darken2);

                                column.Item().PaddingTop(10).AlignCenter().Text("GRADUATION DIPLOMA")
                                    .FontSize(18).Bold();

                                column.Item().PaddingTop(10).AlignCenter().Row(row =>
                                {
                                    row.AutoItem().Text("GPA: ").FontSize(16);
                                    row.AutoItem().Text($"{credential.FinalGrade:F2}")
                                        .FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                                });

                                if (!string.IsNullOrEmpty(credential.Classification))
                                {
                                    column.Item().AlignCenter().Text($"Classification: {credential.Classification}")
                                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken1);
                                }
                            }

                            // Issue Date (Vietnam time: UTC+7)
                            var issuedAtVn = ToVietnamTime(credential.IssuedDate);
                            column.Item().PaddingTop(12).AlignCenter().Text($"Issued on: {issuedAtVn:MMMM dd, yyyy}")
                                .FontSize(14).Italic();

                            // Blockchain Info
                            column.Item().PaddingTop(12).AlignRight().Column(col =>
                            {
                                if (credential.IsOnBlockchain)
                                {
                                    col.Item().AlignRight().Text("✓ Verified on Blockchain")
                                        .FontSize(10).Bold().FontColor(Colors.Green.Darken2);

                                    if (!string.IsNullOrEmpty(credential.BlockchainTransactionHash))
                                    {
                                        col.Item().AlignRight().Text($"TX: {credential.BlockchainTransactionHash.Substring(0, Math.Min(20, credential.BlockchainTransactionHash.Length))}...")
                                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                                    }
                                }
                            });
                        });
                    });
                });

                // Generate PDF
                var pdfBytes = document.GeneratePdf();
                
                _logger.LogInformation("Generated PDF for credential {CredentialId}, Size: {Size} bytes", 
                    credential.CredentialId, pdfBytes.Length);

                return await Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for credential {CredentialId}", credential.CredentialId);
                throw;
            }
        }

        public string GenerateQRCode(string data, int pixelsPerModule = 10)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                var pngByteQrCode = new PngByteQRCode(qrCodeData);
                var bytes = pngByteQrCode.GetGraphic(pixelsPerModule);
                
                return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                throw;
            }
        }

        public byte[] GenerateQRCodeBytes(string data, int pixelsPerModule = 10)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                var pngByteQrCode = new PngByteQRCode(qrCodeData);
                var bytes = pngByteQrCode.GetGraphic(pixelsPerModule);

                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code bytes");
                throw;
            }
        }
    }
}
