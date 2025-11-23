using Fap.Domain.Entities;

namespace Fap.Api.Interfaces
{
    /// <summary>
    /// Service for generating certificate PDFs
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Generate certificate PDF and return bytes
        /// </summary>
        Task<byte[]> GenerateCertificatePdfAsync(Credential credential);

        /// <summary>
        /// Generate QR Code image as base64 string
        /// </summary>
        string GenerateQRCode(string data, int pixelsPerModule = 10);

        /// <summary>
        /// Generate QR Code as PNG bytes
        /// </summary>
        byte[] GenerateQRCodeBytes(string data, int pixelsPerModule = 10);
    }
}
