using TentMan.Domain.Common;

namespace TentMan.Domain.Entities.Identity;

/// <summary>
/// Represents a password reset token with expiration and single-use semantics.
/// Addresses security concerns with using SecurityStamp for password resets.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>
    /// The user ID this token belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The cryptographically secure token string.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When this token expires (UTC).
    /// Typically 1 hour from creation.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Whether this token has been used.
    /// Single-use tokens prevent replay attacks.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Whether this token has been revoked (e.g., user requested new token).
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When this token was used (if applicable).
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Checks if the token is currently valid.
    /// </summary>
    public bool IsValid(DateTime currentTimeUtc) =>
        !IsUsed &&
        !IsRevoked &&
        !IsDeleted &&
        ExpiresAtUtc > currentTimeUtc;
}

/// <summary>
/// Represents an email confirmation token with expiration and single-use semantics.
/// Addresses security concerns with using SecurityStamp for email confirmation.
/// </summary>
public class EmailConfirmationToken : BaseEntity
{
    /// <summary>
    /// The user ID this token belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The cryptographically secure token string.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When this token expires (UTC).
    /// Typically 24-48 hours from creation.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Whether this token has been used.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When this token was used (if applicable).
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Checks if the token is currently valid.
    /// </summary>
    public bool IsValid(DateTime currentTimeUtc) =>
        !IsUsed &&
        !IsRevoked &&
        !IsDeleted &&
        ExpiresAtUtc > currentTimeUtc;
}
