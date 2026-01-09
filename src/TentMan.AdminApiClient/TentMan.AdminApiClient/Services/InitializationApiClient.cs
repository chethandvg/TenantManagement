using TentMan.AdminApiClient.Configuration;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Initialization Admin API client.
/// Provides methods for system initialization operations.
/// </summary>
public sealed class InitializationApiClient : AdminApiClientServiceBase, IInitializationApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="options">The Admin API client options.</param>
    /// <param name="logger">The logger instance.</param>
    public InitializationApiClient(HttpClient httpClient, IOptions<AdminApiClientOptions> options, ILogger<InitializationApiClient> logger)
        : base(httpClient, options, logger)
    {
    }

    /// <inheritdoc/>
    protected override string EndpointName => "initialization";

    /// <inheritdoc/>
    public Task<ApiResponse<InitializationResultDto>> InitializeSystemAsync(
        InitializeSystemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<InitializeSystemRequest, InitializationResultDto>("initialize", request, cancellationToken);
    }
}
