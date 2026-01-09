using TentMan.AdminApiClient.Configuration;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the UserRoles Admin API client.
/// Provides methods for user-role assignment management operations.
/// </summary>
public sealed class UserRolesApiClient : AdminApiClientServiceBase, IUserRolesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRolesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="options">The Admin API client options.</param>
    /// <param name="logger">The logger instance.</param>
    public UserRolesApiClient(HttpClient httpClient, IOptions<AdminApiClientOptions> options, ILogger<UserRolesApiClient> logger)
        : base(httpClient, options, logger)
    {
    }

    /// <inheritdoc/>
    protected override string EndpointName => "userroles";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<RoleDto>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<RoleDto>>(userId.ToString(), cancellationToken);
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
    public Task<ApiResponse<bool>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{userId}/roles/{roleId}", cancellationToken);
    }
}
