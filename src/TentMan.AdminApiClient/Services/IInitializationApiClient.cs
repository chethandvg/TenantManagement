using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Initialization API client.
/// </summary>
/// <remarks>
/// Provides operations for system initialization and setup.
/// These operations bootstrap the application with default roles and a super admin user.
/// </remarks>
public interface IInitializationApiClient
{
    /// <summary>
    /// Initializes the system with all required roles and a super admin user.
    /// </summary>
    /// <param name="request">The initialization request containing super admin credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The initialization result.</returns>
    /// <remarks>
    /// This endpoint should only be called once during initial system setup.
    /// It creates:
    /// - 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
    /// - 1 super admin user with the provided credentials
    /// - Assigns the SuperAdmin role to the created user
    /// 
    /// This endpoint can only be called when no users exist in the system.
    /// After successful initialization, subsequent calls will be rejected.
    /// </remarks>
    Task<ApiResponse<InitializationResult>> InitializeSystemAsync(
        InitializeSystemRequest request,
        CancellationToken cancellationToken = default);
}

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
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
