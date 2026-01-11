using TentMan.Contracts.Authorization;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Authorization API client.
/// Provides methods to check user permissions and policies server-side.
/// </summary>
public sealed class AuthorizationApiClient : ApiClientServiceBase, IAuthorizationApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthorizationApiClient(HttpClient httpClient, ILogger<AuthorizationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/authorization";

    /// <inheritdoc/>
    public Task<ApiResponse<AuthorizationCheckResponse>> CheckPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));
        }

        var request = new CheckPermissionRequest { Permission = permission };
        return PostAsync<CheckPermissionRequest, AuthorizationCheckResponse>(
            "check-permission",
            request,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<AuthorizationCheckResponse>> CheckPolicyAsync(
        string policyName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));
        }

        var request = new CheckPolicyRequest { PolicyName = policyName };
        return PostAsync<CheckPolicyRequest, AuthorizationCheckResponse>(
            "check-policy",
            request,
            cancellationToken);
    }
}
