using TentMan.Domain.Common;

namespace TentMan.Domain.Entities.Identity;

/// <summary>
/// Represents an authenticated user in the system.
/// Inherits from BaseEntity to get auditing, soft delete, and concurrency control.
/// </summary>
public class ApplicationUser : BaseEntity
{
    /// <summary>
    /// Unique username for the user.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address. Used for login and communication.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Normalized email for case-insensitive lookups.
    /// </summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password. Never store plain text passwords.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the email has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Security stamp for invalidating tokens when credentials change.
    /// Should be set explicitly during user creation.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// Current refresh token for JWT authentication.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Expiry time for the refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// Number of failed login attempts (for lockout purposes).
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// Indicates whether the user account is locked out.
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// End date of lockout period. Null if not locked out.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// User's phone number (optional).
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Indicates whether the phone number has been confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Indicates whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Navigation property for user roles (many-to-many relationship).
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Checks if the user is currently locked out.
    /// </summary>
    public bool IsLockedOut => IsLockedOutAt(DateTime.UtcNow);

    /// <summary>
    /// Checks if the user is locked out at the specified time (for testability).
    /// </summary>
    /// <param name="currentTime">The time to check lockout status against.</param>
    /// <returns>True if the user is locked out at the specified time; otherwise, false.</returns>
    public bool IsLockedOutAt(DateTime currentTime)
    {
        return LockoutEnabled && LockoutEnd.HasValue && LockoutEnd.Value > currentTime;
    }
}
