namespace Archu.Contracts.Authentication;

/// <summary>
/// Response model for authentication operations.
/// </summary>
public sealed record AuthenticationResponse
{
    /// <summary>
    /// Gets the access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets the token type (e.g., "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Gets the number of seconds until the token expires.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Gets the user's unique identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's email.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the user's roles.
    /// </summary>
    public IEnumerable<string> Roles { get; init; } = Array.Empty<string>();
}
