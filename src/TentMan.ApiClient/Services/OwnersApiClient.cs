using TentMan.Contracts.Common;
using TentMan.Contracts.Owners;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Owners API client.
/// </summary>
public sealed class OwnersApiClient : ApiClientServiceBase, IOwnersApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OwnersApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public OwnersApiClient(HttpClient httpClient, ILogger<OwnersApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/organizations";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<OwnerDto>>> GetOwnersAsync(
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<OwnerDto>>($"{orgId}/owners", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<OwnerDto>> CreateOwnerAsync(
        Guid orgId,
        CreateOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateOwnerRequest, OwnerDto>($"{orgId}/owners", request, cancellationToken);
    }
}
