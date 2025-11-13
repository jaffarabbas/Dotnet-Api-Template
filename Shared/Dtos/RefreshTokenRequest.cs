using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    /// <summary>
    /// DTO for requesting a new access token using a refresh token.
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
