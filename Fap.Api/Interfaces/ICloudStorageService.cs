using System.Threading.Tasks;

namespace Fap.Api.Interfaces
{
    public interface ICloudStorageService
    {
        /// <summary>
        /// Upload PDF to cloud storage
        /// </summary>
        Task<string> UploadPdfAsync(byte[] pdfBytes, string fileName);

        /// <summary>
        /// Delete PDF from cloud storage
        /// </summary>
        Task<bool> DeletePdfAsync(string fileName);

        /// <summary>
        /// Download PDF from cloud storage
        /// </summary>
        Task<byte[]?> DownloadPdfAsync(string url);

        /// <summary>
        /// Check if file exists
        /// </summary>
        Task<bool> FileExistsAsync(string fileName);
    }
}
