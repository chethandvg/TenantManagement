using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the UserRoles API client.
/// </summary>
/// <remarks>
/// Provides operations for user-role assignment management.
/// </remarks>
public sealed class UserRolesApiClient : AdminApiClientServiceBase, IUserRolesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRolesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public UserRolesApiClient(
        HttpClient httpClient,
        ILogger<UserRolesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/admin/userroles";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<RoleDto>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<RoleDto>>($"{userId}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AssignRoleRequest, object>("assign", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync<object>($"{userId}/roles/{roleId}", cancellationToken);
    }
}
