using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Fap.Api.Interfaces;
using Fap.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class CloudinaryStorageService : ICloudStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;
        private readonly ILogger<CloudinaryStorageService> _logger;

        public CloudinaryStorageService(
            IOptions<CloudinarySettings> settings,
            ILogger<CloudinaryStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Initialize Cloudinary
            var account = new Account(
                _settings.CloudName,
                _settings.ApiKey,
                _settings.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true; // Use HTTPS

            _logger.LogInformation("Cloudinary initialized: {CloudName}", _settings.CloudName);
        }

        public async Task<string> UploadPdfAsync(byte[] pdfBytes, string fileName)
        {
            try
            {
                using var stream = new MemoryStream(pdfBytes);

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    PublicId = $"{_settings.CertificatesFolder}/{Path.GetFileNameWithoutExtension(fileName)}",
                    Folder = _settings.CertificatesFolder,
                    // ResourceType is implicitly Raw for RawUploadParams
                    Overwrite = true,
                    UseFilename = true,
                    UniqueFilename = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                    throw new Exception($"Upload failed: {uploadResult.Error.Message}");
                }

                var url = uploadResult.SecureUrl.ToString();
                _logger.LogInformation("PDF uploaded to Cloudinary: {Url}", url);

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading PDF to Cloudinary: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeletePdfAsync(string fileName)
        {
            try
            {
                var publicId = $"{_settings.CertificatesFolder}/{Path.GetFileNameWithoutExtension(fileName)}";

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Raw
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok" || result.Result == "not found")
                {
                    _logger.LogInformation("PDF deleted from Cloudinary: {FileName}", fileName);
                    return true;
                }

                _logger.LogWarning("Failed to delete PDF from Cloudinary: {Result}", result.Result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PDF from Cloudinary: {FileName}", fileName);
                return false;
            }
        }

        public async Task<byte[]?> DownloadPdfAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(url);

                _logger.LogInformation("PDF downloaded from Cloudinary: {Url}", url);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF from Cloudinary: {Url}", url);
                return null;
            }
        }

        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                var publicId = $"{_settings.CertificatesFolder}/{Path.GetFileNameWithoutExtension(fileName)}";

                var getParams = new GetResourceParams(publicId)
                {
                    ResourceType = ResourceType.Raw
                };

                var result = await _cloudinary.GetResourceAsync(getParams);

                return result != null && result.Error == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FileName}", fileName);
                return false;
            }
        }
    }
}
