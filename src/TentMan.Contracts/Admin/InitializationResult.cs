namespace TentMan.Contracts.Admin;

/// <summary>
/// Represents the result of system initialization.
/// </summary>
public sealed class InitializationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether roles were created.
    /// </summary>
    public bool RolesCreated { get; init; }

    /// <summary>
    /// Gets or sets the number of roles created.
    /// </summary>
    public int RolesCount { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the user was created.
    /// </summary>
    public bool UserCreated { get; init; }

    /// <summary>
    /// Gets or sets the ID of the created user.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
