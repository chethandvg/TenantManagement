using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing email confirmation tokens.
/// Provides secure, time-limited, single-use token operations.
/// </summary>
public class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<EmailConfirmationTokenRepository> _logger;

    public EmailConfirmationTokenRepository(
        ApplicationDbContext context,
        ITimeProvider timeProvider,
        ILogger<EmailConfirmationTokenRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<EmailConfirmationToken?> GetValidTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to get email confirmation token with null or empty token string");
            return null;
        }

        var currentTime = _timeProvider.UtcNow;

        var confirmationToken = await _context.Set<EmailConfirmationToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == token &&
                !t.IsUsed &&
                !t.IsRevoked &&
                !t.IsDeleted &&
                t.ExpiresAtUtc > currentTime,
                cancellationToken);

        if (confirmationToken != null)
        {
            _logger.LogDebug(
                "Valid email confirmation token found for user {UserId}, expires at {ExpiresAt}",
                confirmationToken.UserId,
                confirmationToken.ExpiresAtUtc);
        }
        else
        {
            _logger.LogWarning("No valid email confirmation token found.");
        }

        return confirmationToken;
    }

    /// <inheritdoc/>
    public async Task<EmailConfirmationToken> CreateTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure token
        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = new EmailConfirmationToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('='),
            ExpiresAtUtc = _timeProvider.UtcNow.AddHours(24), // 24 hour expiry for email confirmation
            IsUsed = false,
            IsRevoked = false
        };

        _context.Set<EmailConfirmationToken>().Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created email confirmation token for user {UserId}, expires at {ExpiresAt}",
            userId,
            token.ExpiresAtUtc);

        return token;
    }

    /// <inheritdoc/>
    public async Task MarkAsUsedAsync(
        EmailConfirmationToken token,
        CancellationToken cancellationToken = default)
    {
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        if (token.IsUsed)
        {
            _logger.LogWarning(
                "Attempted to mark already used email confirmation token as used for user {UserId}",
                token.UserId);
            return;
        }

        token.IsUsed = true;
        token.UsedAtUtc = _timeProvider.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked email confirmation token as used for user {UserId}",
            token.UserId);
    }

    /// <inheritdoc/>
    public async Task RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<EmailConfirmationToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!tokens.Any())
        {
            _logger.LogDebug("No active email confirmation tokens found to revoke for user {UserId}", userId);
            return;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked {Count} email confirmation token(s) for user {UserId}",
            tokens.Count,
            userId);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = _timeProvider.UtcNow;

        // Only delete tokens that are expired AND (used OR revoked)
        // Keep valid unused tokens even if expired for audit purposes
        var expiredTokens = await _context.Set<EmailConfirmationToken>()
            .Where(t =>
                t.ExpiresAtUtc < currentTime &&
                (t.IsUsed || t.IsRevoked))
            .ToListAsync(cancellationToken);

        if (!expiredTokens.Any())
        {
            _logger.LogDebug("No expired email confirmation tokens found for cleanup");
            return 0;
        }

        _context.Set<EmailConfirmationToken>().RemoveRange(expiredTokens);
        var deletedCount = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted {Count} expired email confirmation tokens during cleanup",
            deletedCount);

        return deletedCount;
    }
}
