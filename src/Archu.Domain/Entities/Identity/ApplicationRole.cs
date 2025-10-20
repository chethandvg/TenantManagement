using Archu.Domain.Common;

namespace Archu.Domain.Entities.Identity;

/// <summary>
/// Represents a security role in the system.
/// Roles are used to group permissions and control access to resources.
/// </summary>
public class ApplicationRole : BaseEntity
{
    /// <summary>
    /// Unique name of the role (e.g., "Admin", "User", "Manager").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized name for case-insensitive lookups.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the role's purpose and permissions.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for users in this role (many-to-many relationship).
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
