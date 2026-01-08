using TentMan.Application.Common;

namespace TentMan.Application.Abstractions.Authentication;

/// <summary>
/// Defines authentication operations for user registration, login, and token management.
/// This interface abstracts the authentication logic from the application layer,
/// allowing different authentication implementations (JWT, OAuth, etc.).
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password (will be hashed).</param>
    /// <param name="userName">The user's username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing authentication tokens if successful; otherwise, an error message.</returns>
    Task<Result<AuthenticationResult>> RegisterAsync(
        string email,
        string password,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing authentication tokens if successful; otherwise, an error message.</returns>
    Task<Result<AuthenticationResult>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing new authentication tokens if successful; otherwise, an error message.</returns>
    Task<Result<AuthenticationResult>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> LogoutAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a user's email address using a confirmation token.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="confirmationToken">The email confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ConfirmEmailAsync(
        string userId,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset by generating and sending a reset token.
    /// Implementation should return success for non-existent users to prevent email enumeration,
    /// but should return failure for operational issues (e.g., email service unavailable, database errors).
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result indicating success or failure.
    /// Returns success for both valid users (email sent) and non-existent users (silently ignored).
    /// Returns failure only for operational/infrastructure issues.
    /// </returns>
    Task<Result> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="resetToken">The password reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password (requires current password verification).
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a refresh token is valid and not expired.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating whether the token is valid.</returns>
    Task<Result<bool>> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a successful authentication operation.
/// Contains access token, refresh token, and user information.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>
    /// The JWT access token used for API authorization.
    /// Short-lived token (typically 15-60 minutes).
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// The refresh token used to obtain new access tokens.
    /// Long-lived token (typically days or weeks).
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// The date and time when the access token expires (UTC).
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// The date and time when the refresh token expires (UTC).
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// The type of token (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// User information associated with the authentication.
    /// </summary>
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// Contains basic user information returned after authentication.
/// </summary>
public sealed class UserInfo
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The user's username.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the user's email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { get; init; } = Array.Empty<string>();
}
