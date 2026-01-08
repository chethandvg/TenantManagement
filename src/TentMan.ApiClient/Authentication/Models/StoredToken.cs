namespace TentMan.ApiClient.Authentication.Models;

/// <summary>
/// Represents the stored token information.
/// </summary>
public sealed record StoredToken
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
    /// Gets the expiration time in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// Determines if the token is expired or will expire within the specified buffer.
    /// </summary>
    /// <param name="bufferSeconds">Buffer time in seconds before actual expiration.</param>
    /// <returns>True if the token is expired or will expire soon.</returns>
    public bool IsExpired(int bufferSeconds = 60) =>
        DateTime.UtcNow >= ExpiresAtUtc.AddSeconds(-bufferSeconds);

    /// <summary>
    /// Creates a StoredToken from a TokenResponse.
    /// </summary>
    public static StoredToken FromTokenResponse(TokenResponse tokenResponse) => new()
    {
        AccessToken = tokenResponse.AccessToken,
        RefreshToken = tokenResponse.RefreshToken,
        TokenType = tokenResponse.TokenType,
        ExpiresAtUtc = tokenResponse.ExpiresAt
    };
}
