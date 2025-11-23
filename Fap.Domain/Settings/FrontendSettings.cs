using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.Settings
{
    /// <summary>
    /// Cấu hình cho Frontend URL - dùng khi tạo link chia sẻ chứng chỉ và QR Code
    /// </summary>
    public class FrontendSettings
    {
        /// <summary>
        /// Base URL của Frontend (VD: https://fap-portal.vercel.app)
        /// </summary>
        [Required]
        public string BaseUrl { get; set; } = "https://uapblockchain.vercel.app";

        /// <summary>
        /// Đường dẫn để xác thực chứng chỉ (VD: certificates/verify)
        /// </summary>
        [Required]
        public string VerifyPath { get; set; } = "certificates/verify";

        /// <summary>
        /// Kích thước QR Code mặc định (pixels)
        /// </summary>
        [Range(120, 1024)]
        public int DefaultQrSize { get; set; } = 300;
    }
}
