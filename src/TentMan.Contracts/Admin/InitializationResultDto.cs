namespace TentMan.Contracts.Admin;

/// <summary>
/// Result of system initialization.
/// </summary>
public sealed class InitializationResultDto
{
    /// <summary>
    /// Indicates whether roles were created during initialization.
    /// </summary>
    public bool RolesCreated { get; init; }

    /// <summary>
    /// The number of roles created.
    /// </summary>
    public int RolesCount { get; init; }

    /// <summary>
    /// Indicates whether the super admin user was created.
    /// </summary>
    public bool UserCreated { get; init; }

    /// <summary>
    /// The ID of the created super admin user, if any.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Indicates whether an organization was created during initialization.
    /// </summary>
    public bool OrganizationCreated { get; init; }

    /// <summary>
    /// The ID of the created organization, if any.
    /// </summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>
    /// Indicates whether an owner was created during initialization.
    /// </summary>
    public bool OwnerCreated { get; init; }

    /// <summary>
    /// The ID of the created owner, if any.
    /// </summary>
    public Guid? OwnerId { get; init; }

    /// <summary>
    /// A message describing the result of the initialization.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
