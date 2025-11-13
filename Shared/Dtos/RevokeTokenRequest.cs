using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    /// <summary>
    /// DTO for revoking a refresh token.
    /// </summary>
    public class RevokeTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
