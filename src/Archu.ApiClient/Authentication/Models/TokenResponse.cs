namespace Archu.ApiClient.Authentication.Models;

/// <summary>
/// Represents a token response from the authentication API.
/// </summary>
public sealed record TokenResponse
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
    /// Gets the timestamp when the token was issued.
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the calculated expiration time.
    /// </summary>
    public DateTime ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);

    /// <summary>
    /// Determines if the token is expired or will expire within the specified buffer.
    /// </summary>
    /// <param name="bufferSeconds">Buffer time in seconds before actual expiration.</param>
    /// <returns>True if the token is expired or will expire soon.</returns>
    public bool IsExpired(int bufferSeconds = 60) =>
        DateTime.UtcNow >= ExpiresAt.AddSeconds(-bufferSeconds);
}
