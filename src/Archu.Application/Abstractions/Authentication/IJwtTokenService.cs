using System.Security.Claims;

namespace Archu.Application.Abstractions.Authentication;

/// <summary>
/// Service for generating and validating JWT tokens.
/// Handles creation of access tokens and refresh tokens for authentication.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for an authenticated user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="userName">The user's username.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>A JWT access token string.</returns>
    string GenerateAccessToken(
        string userId,
        string email,
        string userName,
        IEnumerable<string> roles);

    /// <summary>
    /// Generates a secure refresh token.
    /// Refresh tokens are cryptographically random strings (not JWTs).
    /// </summary>
    /// <returns>A secure refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>Claims principal if valid; otherwise, null.</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the expiration time for access tokens.
    /// </summary>
    /// <returns>Access token expiration as a TimeSpan.</returns>
    TimeSpan GetAccessTokenExpiration();

    /// <summary>
    /// Gets the expiration time for refresh tokens.
    /// </summary>
    /// <returns>Refresh token expiration as a TimeSpan.</returns>
    TimeSpan GetRefreshTokenExpiration();

    /// <summary>
    /// Calculates the UTC expiration date/time for an access token created now.
    /// </summary>
    /// <returns>The UTC date/time when an access token created now will expire.</returns>
    DateTime GetAccessTokenExpiryUtc();

    /// <summary>
    /// Calculates the UTC expiration date/time for a refresh token created now.
    /// </summary>
    /// <returns>The UTC date/time when a refresh token created now will expire.</returns>
    DateTime GetRefreshTokenExpiryUtc();
}
