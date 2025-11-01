using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Handles refresh token operations including storage, validation, and rotation.
/// Implements security best practices for refresh token management.
/// </summary>
public sealed class RefreshTokenHandler
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IJwtTokenService jwtTokenService,
        ITimeProvider timeProvider,
        ILogger<RefreshTokenHandler> logger)
    {
        _jwtTokenService = jwtTokenService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Generates and stores a new refresh token for a user.
    /// </summary>
    /// <param name="user">The user to generate a refresh token for.</param>
    /// <returns>The generated refresh token and its expiry time.</returns>
    public (string refreshToken, DateTime expiresAt) GenerateAndStoreRefreshToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetRefreshTokenExpiryUtc();

        // Store refresh token in user entity
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = expiresAt;

        _logger.LogDebug(
            "Generated new refresh token for user {UserId}, expires at {ExpiresAt}",
            user.Id,
            expiresAt);

        return (refreshToken, expiresAt);
    }

    /// <summary>
    /// Validates a refresh token for a specific user.
    /// </summary>
    /// <param name="user">The user whose refresh token to validate.</param>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>True if the refresh token is valid; otherwise, false.</returns>
    public bool ValidateRefreshToken(ApplicationUser user, string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Refresh token validation failed: Token is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.RefreshToken))
        {
            _logger.LogWarning(
                "Refresh token validation failed for user {UserId}: No refresh token stored",
                user.Id);
            return false;
        }

        // Check if tokens match
        if (user.RefreshToken != refreshToken)
        {
            _logger.LogWarning(
                "Refresh token validation failed for user {UserId}: Token mismatch",
                user.Id);
            return false;
        }

        // Check if token is expired
        if (!user.RefreshTokenExpiryTime.HasValue ||
            user.RefreshTokenExpiryTime.Value <= _timeProvider.UtcNow)
        {
            _logger.LogWarning(
                "Refresh token validation failed for user {UserId}: Token expired at {ExpiryTime}",
                user.Id,
                user.RefreshTokenExpiryTime);
            return false;
        }

        _logger.LogDebug(
            "Refresh token validated successfully for user {UserId}",
            user.Id);

        return true;
    }

    /// <summary>
    /// Revokes a user's refresh token (for logout or security reasons).
    /// </summary>
    /// <param name="user">The user whose refresh token to revoke.</param>
    public void RevokeRefreshToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        _logger.LogInformation(
            "Revoked refresh token for user {UserId}",
            user.Id);
    }

    /// <summary>
    /// Performs refresh token rotation: generates new token and invalidates old one.
    /// This is a security best practice to prevent refresh token reuse.
    /// </summary>
    /// <param name="user">The user to rotate the refresh token for.</param>
    /// <returns>The new refresh token and its expiry time.</returns>
    public (string refreshToken, DateTime expiresAt) RotateRefreshToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _logger.LogInformation(
            "Rotating refresh token for user {UserId}",
            user.Id);

        // Generate new refresh token (automatically replaces old one)
        return GenerateAndStoreRefreshToken(user);
    }

    /// <summary>
    /// Cleans up expired refresh tokens for security housekeeping.
    /// Should be called periodically (e.g., via background service).
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of expired tokens cleared.</returns>
    public async Task<int> CleanupExpiredTokensAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.UtcNow;

        var expiredUsers = await dbContext.Set<ApplicationUser>()
            .Where(u => u.RefreshToken != null &&
                        u.RefreshTokenExpiryTime.HasValue &&
                        u.RefreshTokenExpiryTime.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var user in expiredUsers)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        if (expiredUsers.Any())
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired refresh tokens",
                expiredUsers.Count);
        }

        return expiredUsers.Count;
    }
}
