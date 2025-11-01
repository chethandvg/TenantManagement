using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing password reset tokens.
/// Provides secure, time-limited, single-use token operations.
/// </summary>
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<PasswordResetTokenRepository> _logger;

    public PasswordResetTokenRepository(
        ApplicationDbContext context,
        ITimeProvider timeProvider,
        ILogger<PasswordResetTokenRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PasswordResetToken?> GetValidTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to get password reset token with null or empty token string");
            return null;
        }

        var currentTime = _timeProvider.UtcNow;

        var resetToken = await _context.Set<PasswordResetToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == token &&
                !t.IsUsed &&
                !t.IsRevoked &&
                !t.IsDeleted &&
                t.ExpiresAtUtc > currentTime,
                cancellationToken);

        if (resetToken != null)
        {
            _logger.LogDebug(
                "Valid password reset token found for user {UserId}, expires at {ExpiresAt}",
                resetToken.UserId,
                resetToken.ExpiresAtUtc);
        }
        else
        {
            _logger.LogWarning("No valid password reset token found.");
        }

        return resetToken;
    }

    /// <inheritdoc/>
    public async Task<PasswordResetToken> CreateTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure token
        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = new PasswordResetToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('='),
            ExpiresAtUtc = _timeProvider.UtcNow.AddHours(1), // 1 hour expiry for password reset
            IsUsed = false,
            IsRevoked = false
        };

        _context.Set<PasswordResetToken>().Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created password reset token for user {UserId}, expires at {ExpiresAt}",
            userId,
            token.ExpiresAtUtc);

        return token;
    }

    /// <inheritdoc/>
    public async Task MarkAsUsedAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        if (token.IsUsed)
        {
            _logger.LogWarning(
                "Attempted to mark already used password reset token as used for user {UserId}",
                token.UserId);
            return;
        }

        token.IsUsed = true;
        token.UsedAtUtc = _timeProvider.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked password reset token as used for user {UserId}",
            token.UserId);
    }

    /// <inheritdoc/>
    public async Task RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!tokens.Any())
        {
            _logger.LogDebug("No active password reset tokens found to revoke for user {UserId}", userId);
            return;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked {Count} password reset token(s) for user {UserId}",
            tokens.Count,
            userId);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = _timeProvider.UtcNow;

        // Only delete tokens that are expired AND (used OR revoked)
        // Keep valid unused tokens even if expired for audit purposes
        var expiredTokens = await _context.Set<PasswordResetToken>()
            .Where(t =>
                t.ExpiresAtUtc < currentTime &&
                (t.IsUsed || t.IsRevoked))
            .ToListAsync(cancellationToken);

        if (!expiredTokens.Any())
        {
            _logger.LogDebug("No expired password reset tokens found for cleanup");
            return 0;
        }

        _context.Set<PasswordResetToken>().RemoveRange(expiredTokens);
        var deletedCount = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted {Count} expired password reset tokens during cleanup",
            deletedCount);

        return deletedCount;
    }
}
