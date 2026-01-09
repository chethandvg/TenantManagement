using TentMan.AdminApiClient.Configuration;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Users Admin API client.
/// Provides methods for user management operations.
/// </summary>
public sealed class UsersApiClient : AdminApiClientServiceBase, IUsersApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="options">The Admin API client options.</param>
    /// <param name="logger">The logger instance.</param>
    public UsersApiClient(HttpClient httpClient, IOptions<AdminApiClientOptions> options, ILogger<UsersApiClient> logger)
        : base(httpClient, options, logger)
    {
    }

    /// <inheritdoc/>
    protected override string EndpointName => "users";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"?pageNumber={pageNumber}&pageSize={pageSize}";
        return GetAsync<IEnumerable<UserDto>>(endpoint, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateUserRequest, UserDto>(string.Empty, request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync(userId.ToString(), cancellationToken);
    }
}
