using System.ComponentModel.DataAnnotations;
using Archu.Domain.Abstractions;

namespace Archu.Domain.Entities.Identity;

/// <summary>
/// Junction entity mapping permissions directly to users for scenario-specific overrides.
/// Captures auditing metadata to track lifecycle changes.
/// </summary>
public class UserPermission : IAuditable, ISoftDeletable
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

    /// <summary>
    /// UTC timestamp when the record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Identifier for the actor that created the record.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the record was last modified.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Identifier for the actor that last modified the record.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Indicates whether the record has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the record was soft-deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Identifier for the actor that performed the soft delete, if applicable.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
