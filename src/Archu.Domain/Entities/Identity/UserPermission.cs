using Archu.Domain.Common;

namespace Archu.Domain.Entities.Identity;

/// <summary>
/// Junction entity mapping permissions directly to users for scenario-specific overrides.
/// Inherits from <see cref="BaseEntity"/> to capture auditing and soft-delete metadata.
/// </summary>
public class UserPermission : BaseEntity
{
    /// <summary>
    /// Foreign key to the user receiving the permission.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the associated user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Foreign key to the permission assigned to the user.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Navigation property to the associated permission.
    /// </summary>
    public ApplicationPermission Permission { get; set; } = null!;
}
