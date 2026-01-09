using TentMan.AdminApiClient.Configuration;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Roles Admin API client.
/// Provides methods for role management operations.
/// </summary>
public sealed class RolesApiClient : AdminApiClientServiceBase, IRolesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="options">The Admin API client options.</param>
    /// <param name="logger">The logger instance.</param>
    public RolesApiClient(HttpClient httpClient, IOptions<AdminApiClientOptions> options, ILogger<RolesApiClient> logger)
        : base(httpClient, options, logger)
    {
    }

    /// <inheritdoc/>
    protected override string EndpointName => "roles";

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
