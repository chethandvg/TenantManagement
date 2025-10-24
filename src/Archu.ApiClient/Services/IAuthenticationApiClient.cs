using Archu.Contracts.Authentication;
using Archu.Contracts.Common;

namespace Archu.ApiClient.Services;

/// <summary>
/// Interface for the Authentication API client.
/// </summary>
public interface IAuthenticationApiClient
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<ApiResponse<AuthenticationResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with credentials.
    /// </summary>
    Task<ApiResponse<AuthenticationResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    Task<ApiResponse<AuthenticationResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    Task<ApiResponse<object>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset.
    /// </summary>
    Task<ApiResponse<object>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a password with a token.
    /// </summary>
    Task<ApiResponse<object>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a user's email address.
    /// </summary>
    Task<ApiResponse<object>> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default);
}
