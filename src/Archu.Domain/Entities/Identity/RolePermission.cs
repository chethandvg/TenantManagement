using Archu.Domain.Common;

namespace Archu.Domain.Entities.Identity;

/// <summary>
/// Junction entity mapping permissions to roles to enable granular authorization policies.
/// Inherits from <see cref="BaseEntity"/> to reuse auditing, soft delete, and concurrency columns.
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Foreign key to the role receiving the permission.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation property to the associated role.
    /// </summary>
    public ApplicationRole Role { get; set; } = null!;

    /// <summary>
    /// Foreign key to the permission granted to the role.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Navigation property to the associated permission.
    /// </summary>
    public ApplicationPermission Permission { get; set; } = null!;
}
