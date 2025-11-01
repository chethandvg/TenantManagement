using Archu.Domain.Common;

namespace Archu.Domain.Entities.Identity;

/// <summary>
/// Represents a discrete permission that can be granted to roles or users.
/// Permissions provide fine-grained access control beyond role assignments.
/// </summary>
public class ApplicationPermission : BaseEntity
{
    /// <summary>
    /// Human-readable name of the permission (e.g., "CreateInvoice").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized name for case-insensitive lookups and comparisons.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description explaining the purpose of the permission.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for roles associated with this permission.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Navigation property for users directly assigned this permission.
    /// </summary>
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
