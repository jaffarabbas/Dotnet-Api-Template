namespace Shared.Dtos
{
    /// <summary>
    /// Response DTO containing new access token and refresh token.
    /// </summary>
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
