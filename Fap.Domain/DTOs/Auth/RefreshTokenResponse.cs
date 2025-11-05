namespace Fap.Domain.DTOs.Auth
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
    }
}