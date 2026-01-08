using TentMan.Domain.Entities.Identity;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for password reset token management.
/// Provides secure, time-limited, single-use token operations.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Gets a valid (non-expired, non-used, non-revoked) password reset token.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token if found and valid; otherwise, null.</returns>
    Task<PasswordResetToken?> GetValidTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new password reset token for a user.
    /// Automatically sets expiration (1 hour from now).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token.</returns>
    Task<PasswordResetToken> CreateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a token as used.
    /// Prevents token reuse.
    /// </summary>
    /// <param name="token">The token to mark as used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all outstanding password reset tokens for a user.
    /// Called when user changes password or requests new token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired tokens (housekeeping).
    /// Should be called periodically via background job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for email confirmation token management.
/// Provides secure, time-limited, single-use token operations.
/// </summary>
public interface IEmailConfirmationTokenRepository
{
    /// <summary>
    /// Gets a valid (non-expired, non-used, non-revoked) email confirmation token.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token if found and valid; otherwise, null.</returns>
    Task<EmailConfirmationToken?> GetValidTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new email confirmation token for a user.
    /// Automatically sets expiration (24 hours from now).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token.</returns>
    Task<EmailConfirmationToken> CreateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a token as used.
    /// Prevents token reuse.
    /// </summary>
    /// <param name="token">The token to mark as used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsUsedAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all outstanding email confirmation tokens for a user.
    /// Called when user requests new confirmation email.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired tokens (housekeeping).
    /// Should be called periodically via background job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens deleted.</returns>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}
