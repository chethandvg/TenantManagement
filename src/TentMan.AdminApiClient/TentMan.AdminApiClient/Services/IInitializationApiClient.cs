using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Initialization Admin API client.
/// Provides methods for system initialization operations.
/// </summary>
public interface IInitializationApiClient
{
    /// <summary>
    /// Initializes the system with all required roles and a super admin user.
    /// This endpoint should only be called once during initial system setup.
    /// </summary>
    /// <param name="request">The initialization request containing super admin credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The initialization result.</returns>
    /// <remarks>
    /// This endpoint creates:
    /// - 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
    /// - 1 super admin user with the provided credentials
    /// - Assigns the SuperAdmin role to the created user
    /// 
    /// **IMPORTANT**: 
    /// - This endpoint can only be called when no users exist in the system
    /// - After successful initialization, subsequent calls will be rejected
    /// </remarks>
    Task<ApiResponse<InitializationResultDto>> InitializeSystemAsync(
        InitializeSystemRequest request,
        CancellationToken cancellationToken = default);
}
