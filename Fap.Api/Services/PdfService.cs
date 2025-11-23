using Fap.Api.Interfaces;
using Fap.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Fap.Api.Services
{
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;

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
                // Generate QR Code for the certificate
                var qrCodeData = credential.ShareableUrl ?? $"https://verify.certificate/{credential.CredentialId}";
                var qrCodeBytes = GenerateQRCodeBytes(qrCodeData, 10);

                // Create PDF document
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Page settings
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(50);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black));

                        // Header
                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("FPT UNIVERSITY")
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
                            column.Spacing(15);

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

                            // Issue Date
                            column.Item().PaddingTop(20).AlignCenter().Text($"Issued on: {credential.IssuedDate:MMMM dd, yyyy}")
                                .FontSize(14).Italic();

                            // QR Code and Blockchain Info
                            column.Item().PaddingTop(20).Row(row =>
                            {
                                // Left: QR Code
                                row.RelativeItem(1).Column(col =>
                                {
                                    col.Item().AlignLeft().Text("Scan to verify:")
                                        .FontSize(10).Italic();
                                    col.Item().AlignLeft().PaddingTop(5).Image(qrCodeBytes)
                                        .FitWidth();
                                });

                                // Center: Spacer
                                row.RelativeItem(2);

                                // Right: Blockchain Info
                                row.RelativeItem(1).Column(col =>
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
                                    else
                                    {
                                        col.Item().AlignRight().Text("⏳ Blockchain Pending")
                                            .FontSize(10).Italic().FontColor(Colors.Orange.Medium);
                                    }
                                });
                            });
                        });

                        // Footer
                        page.Footer().Column(column =>
                        {
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().PaddingTop(10).Row(row =>
                            {
                                var hashLength = credential.VerificationHash?.Length ?? 0;
                                var hashPreview = hashLength > 0 
                                    ? credential.VerificationHash!.Substring(0, Math.Min(32, hashLength)) + "..."
                                    : "N/A";
                                
                                row.RelativeItem().AlignLeft().Text($"Verification Hash: {hashPreview}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                                row.RelativeItem().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
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
                using var qrCode = new QRCode(qrCodeData);
                using var bitmap = qrCode.GetGraphic(pixelsPerModule);
                
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = stream.ToArray();
                
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
                using var qrCode = new QRCode(qrCodeData);
                using var bitmap = qrCode.GetGraphic(pixelsPerModule);
                
                using var stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code bytes");
                throw;
            }
        }
    }
}
