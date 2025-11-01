using System.Security.Claims;
using Archu.Application.Common;

namespace Archu.Application.Abstractions.Authentication;

/// <summary>
/// Defines JWT token operations for generating, validating, and managing tokens.
/// This interface abstracts token management logic from the application layer.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="userName">The user's username.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <param name="additionalClaims">Optional additional claims to include in the token.</param>
    /// <returns>A JWT access token string.</returns>
    string GenerateAccessToken(
        string userId,
        string email,
        string userName,
        IEnumerable<string> roles,
        IDictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Generates a secure refresh token.
    /// </summary>
    /// <returns>A cryptographically secure refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and returns the claims principal if valid.
    /// </summary>
    /// <param name="token">The JWT access token to validate.</param>
    /// <returns>A result containing the claims principal if valid; otherwise, an error message.</returns>
    Result<ClaimsPrincipal> ValidateAccessToken(string token);

    /// <summary>
    /// Extracts the user identifier from a JWT access token without full validation.
    /// Useful for extracting user ID from expired tokens during refresh.
    /// </summary>
    /// <param name="token">The JWT access token.</param>
    /// <returns>A result containing the user ID if extraction succeeds; otherwise, an error message.</returns>
    Result<string> GetUserIdFromToken(string token);

    /// <summary>
    /// Extracts all claims from a JWT access token.
    /// </summary>
    /// <param name="token">The JWT access token.</param>
    /// <returns>A result containing the claims if extraction succeeds; otherwise, an error message.</returns>
    Result<IEnumerable<Claim>> GetClaimsFromToken(string token);

    /// <summary>
    /// Gets the remaining time until the token expires.
    /// </summary>
    /// <param name="token">The JWT access token.</param>
    /// <returns>A result containing the remaining time if valid; otherwise, an error message.</returns>
    Result<TimeSpan> GetTokenRemainingLifetime(string token);

    /// <summary>
    /// Checks if a JWT access token is expired.
    /// </summary>
    /// <param name="token">The JWT access token.</param>
    /// <returns>True if the token is expired; otherwise, false.</returns>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Generates a secure token for email confirmation.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A secure confirmation token string.</returns>
    string GenerateEmailConfirmationToken(string userId);

    /// <summary>
    /// Generates a secure token for password reset.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A secure password reset token string.</returns>
    string GeneratePasswordResetToken(string userId);

    /// <summary>
    /// Validates an email confirmation token.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="token">The confirmation token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool ValidateEmailConfirmationToken(string userId, string token);

    /// <summary>
    /// Validates a password reset token.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="token">The reset token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool ValidatePasswordResetToken(string userId, string token);
}

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// The secret key used to sign JWT tokens.
    /// Should be at least 256 bits (32 characters) for HS256 algorithm.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// The issuer of the JWT token (typically your application name or domain).
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// The audience for the JWT token (typically your API domain or identifier).
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// The lifetime of the access token in minutes.
    /// Recommended: 15-60 minutes for security.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// The lifetime of the refresh token in days.
    /// Recommended: 7-30 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;

    /// <summary>
    /// Whether to validate the token issuer.
    /// </summary>
    public bool ValidateIssuer { get; init; } = true;

    /// <summary>
    /// Whether to validate the token audience.
    /// </summary>
    public bool ValidateAudience { get; init; } = true;

    /// <summary>
    /// Whether to validate the token lifetime.
    /// </summary>
    public bool ValidateLifetime { get; init; } = true;

    /// <summary>
    /// Whether to validate the token signature.
    /// </summary>
    public bool ValidateIssuerSigningKey { get; init; } = true;

    /// <summary>
    /// Clock skew to allow for small time differences between servers.
    /// </summary>
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(5);
}
