using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Implementation of the Users API client.
/// </summary>
/// <remarks>
/// Provides operations for user management.
/// </remarks>
public sealed class UsersApiClient : AdminApiClientServiceBase, IUsersApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public UsersApiClient(
        HttpClient httpClient,
        ILogger<UsersApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/admin/users";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<UserDto>>(
            $"?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);
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
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{id}", cancellationToken);
    }
}
