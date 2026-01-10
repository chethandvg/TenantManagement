using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking changes to sensitive entities.
/// Records who made changes, what was changed, and when.
/// </summary>
public class AuditLog : BaseEntity
{
    public AuditLog()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    /// <summary>
    /// The ID of the user who made the change.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The username who made the change.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The type of entity that was changed (e.g., "Lease", "LeaseTerm", "DepositTransaction").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity that was changed.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// The action performed (Create, Update, Delete).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The state of the entity before the change (JSON serialized).
    /// Null for Create operations.
    /// </summary>
    public string? BeforeState { get; set; }

    /// <summary>
    /// The state of the entity after the change (JSON serialized).
    /// Null for Delete operations.
    /// </summary>
    public string? AfterState { get; set; }

    /// <summary>
    /// List of changed property names (comma-separated).
    /// </summary>
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// The organization ID associated with the change.
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// IP address of the user who made the change.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client that made the change.
    /// </summary>
    public string? UserAgent { get; set; }
}
