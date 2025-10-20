using System.ComponentModel.DataAnnotations;

namespace Archu.Contracts.Authentication;

/// <summary>
/// Request model for refreshing authentication tokens.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;
}
