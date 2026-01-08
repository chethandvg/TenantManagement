using TentMan.Contracts.Authentication;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Authentication API client.
/// </summary>
public sealed class AuthenticationApiClient : ApiClientServiceBase, IAuthenticationApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthenticationApiClient(HttpClient httpClient, ILogger<AuthenticationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/authentication";

    /// <inheritdoc/>
    public Task<ApiResponse<AuthenticationResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<RegisterRequest, AuthenticationResponse>("register", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<AuthenticationResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<LoginRequest, AuthenticationResponse>("login", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<AuthenticationResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<RefreshTokenRequest, AuthenticationResponse>("refresh-token", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        return PostAsync<object, object>("logout", new { }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<ChangePasswordRequest, object>("change-password", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<ForgotPasswordRequest, object>("forgot-password", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<ResetPasswordRequest, object>("reset-password", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<object>> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<ConfirmEmailRequest, object>("confirm-email", request, cancellationToken);
    }
}
