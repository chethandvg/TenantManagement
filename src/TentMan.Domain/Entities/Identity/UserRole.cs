namespace TentMan.Domain.Entities.Identity;

/// <summary>
/// Junction entity for the many-to-many relationship between users and roles.
/// This allows users to have multiple roles and roles to contain multiple users.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Foreign key to the ApplicationUser.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Foreign key to the ApplicationRole.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation property to the role.
    /// </summary>
    public ApplicationRole Role { get; set; } = null!;

    /// <summary>
    /// Timestamp when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; }

    /// <summary>
    /// Who assigned this role (audit trail).
    /// </summary>
    public string? AssignedBy { get; set; }
}
