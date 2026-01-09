using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Roles API client.
/// </summary>
/// <remarks>
/// Provides operations for role management.
/// </remarks>
public sealed class RolesApiClient : AdminApiClientServiceBase, IRolesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public RolesApiClient(
        HttpClient httpClient,
        ILogger<RolesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/admin/roles";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<RoleDto>>(string.Empty, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<RoleDto>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateRoleRequest, RoleDto>(string.Empty, request, cancellationToken);
    }
}
