using TentMan.Application.Admin.Commands.InitializeSystem;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Initialization API client.
/// </summary>
/// <remarks>
/// Provides operations for system initialization and setup.
/// </remarks>
public sealed class InitializationApiClient : AdminApiClientServiceBase, IInitializationApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public InitializationApiClient(
        HttpClient httpClient,
        ILogger<InitializationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/admin/initialization";

    /// <inheritdoc/>
    public Task<ApiResponse<InitializationResult>> InitializeSystemAsync(
        InitializeSystemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<InitializeSystemRequest, InitializationResult>(
            "initialize",
            request,
            cancellationToken);
    }
}
