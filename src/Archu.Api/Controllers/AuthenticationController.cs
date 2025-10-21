using Archu.Application.Abstractions.Authentication;
using Archu.Contracts.Authentication;
using Archu.Contracts.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.Api.Controllers;

/// <summary>
/// Controller for authentication operations (login, register, refresh token, etc.).
/// Provides secure JWT-based authentication endpoints.
/// </summary>
/// <remarks>
/// This controller handles all authentication-related operations including:
/// - User registration
/// - User login (email/password)
/// - Token refresh
/// - Password management (change, reset, forgot)
/// - Email confirmation
/// - Logout (token revocation)
/// 
/// All endpoints use standardized ApiResponse wrapper for consistent error handling.
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/register
    ///     {
    ///         "email": "user@example.com",
    ///         "password": "SecurePassword123!",
    ///         "userName": "johndoe"
    ///     }
    /// 
    /// Password requirements:
    /// - Minimum 8 characters
    /// - Maximum 100 characters
    /// 
    /// Returns JWT access token and refresh token upon successful registration.
    /// </remarks>
    /// <param name="request">Registration details (email, password, username).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication tokens and user information if successful.</returns>
    /// <response code="200">Registration successful. Returns JWT tokens and user info.</response>
    /// <response code="400">Validation failed or user already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var result = await _authenticationService.RegisterAsync(
            request.Email,
            request.Password,
            request.UserName,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Registration failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        return Ok(ApiResponse<AuthenticationResult>.Ok(result.Value!, "Registration successful"));
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/login
    ///     {
    ///         "email": "user@example.com",
    ///         "password": "SecurePassword123!"
    ///     }
    /// 
    /// Returns JWT access token (valid for 1 hour by default) and refresh token (valid for 7 days).
    /// Access token should be included in subsequent API requests in the Authorization header.
    /// </remarks>
    /// <param name="request">Login credentials (email and password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication tokens and user information if successful.</returns>
    /// <response code="200">Login successful. Returns JWT tokens and user info.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var result = await _authenticationService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Login failed for {Email}: {Error}", request.Email, result.Error);
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(ApiResponse<AuthenticationResult>.Ok(result.Value!, "Login successful"));
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/refresh-token
    ///     {
    ///         "refreshToken": "base64-encoded-refresh-token"
    ///     }
    /// 
    /// Use this endpoint when the access token expires (typically after 1 hour).
    /// Returns a new access token and refresh token pair.
    /// Old refresh token is invalidated after successful refresh.
    /// </remarks>
    /// <param name="request">Refresh token obtained from login or previous refresh.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication tokens if successful.</returns>
    /// <response code="200">Token refreshed successfully. Returns new JWT tokens.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refresh token request received");

        var result = await _authenticationService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token refresh failed: {Error}", result.Error);
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogDebug("Token refreshed successfully");
        return Ok(ApiResponse<AuthenticationResult>.Ok(result.Value!, "Token refreshed successfully"));
    }

    /// <summary>
    /// Logs out the current user by revoking their refresh token.
    /// </summary>
    /// <remarks>
    /// Requires valid JWT token in Authorization header.
    /// Invalidates the refresh token to prevent future token refreshes.
    /// Client should discard both access and refresh tokens.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">User not authenticated (invalid or missing token).</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Logout attempt with no user ID in claims");
            return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));
        }

        _logger.LogInformation("Logout request for user: {UserId}", userId);

        var result = await _authenticationService.LogoutAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Logout failed for {UserId}: {Error}", userId, result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("User logged out successfully: {UserId}", userId);
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully"));
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <remarks>
    /// Requires authentication. User must provide current password for verification.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/change-password
    ///     Authorization: Bearer {token}
    ///     {
    ///         "currentPassword": "OldPassword123!",
    ///         "newPassword": "NewSecurePassword456!"
    ///     }
    /// </remarks>
    /// <param name="request">Current password and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Invalid current password or validation failed.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Change password attempt with no user ID in claims");
            return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));
        }

        _logger.LogInformation("Change password request for user: {UserId}", userId);

        var result = await _authenticationService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Change password failed for {UserId}: {Error}", userId, result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully"));
    }

    /// <summary>
    /// Initiates a password reset by sending a reset token to the user's email.
    /// </summary>
    /// <remarks>
    /// Always returns success to prevent email enumeration attacks.
    /// If email exists, user will receive reset instructions via email.
    /// Reset token is valid for limited time (typically 1 hour).
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/forgot-password
    ///     {
    ///         "email": "user@example.com"
    ///     }
    /// </remarks>
    /// <param name="request">User's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response (always returns success for security).</returns>
    /// <response code="200">Request processed. If email exists, reset instructions sent.</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        var result = await _authenticationService.ForgotPasswordAsync(
            request.Email,
            cancellationToken);

        // Always return success to prevent email enumeration
        // The service layer handles the logic of sending emails only to valid users
        if (!result.IsSuccess)
        {
            _logger.LogError("Forgot password operation failed: {Error}", result.Error);
            // Return success anyway for security (but log the actual error)
        }

        return Ok(ApiResponse<object>.Ok(
            new { },
            "If your email exists in our system, you will receive a password reset link."));
    }

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <remarks>
    /// Use the reset token received via email from forgot-password endpoint.
    /// Token is single-use and expires after limited time.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/reset-password
    ///     {
    ///         "email": "user@example.com",
    ///         "resetToken": "token-from-email",
    ///         "newPassword": "NewSecurePassword123!"
    ///     }
    /// </remarks>
    /// <param name="request">Email, reset token, and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Invalid or expired token, or validation failed.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

        var result = await _authenticationService.ResetPasswordAsync(
            request.Email,
            request.ResetToken,
            request.NewPassword,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Password reset failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("Password reset successfully for: {Email}", request.Email);
        return Ok(ApiResponse<object>.Ok(new { }, "Password reset successfully"));
    }

    /// <summary>
    /// Confirms a user's email address.
    /// </summary>
    /// <remarks>
    /// Use the confirmation token received via email during registration.
    /// Required before certain operations (if email verification is enabled).
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/authentication/confirm-email
    ///     {
    ///         "userId": "user-guid",
    ///         "confirmationToken": "token-from-email"
    ///     }
    /// </remarks>
    /// <param name="request">User ID and confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
    /// <response code="200">Email confirmed successfully.</response>
    /// <response code="400">Invalid or expired token.</response>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email confirmation attempt for user: {UserId}", request.UserId);

        var result = await _authenticationService.ConfirmEmailAsync(
            request.UserId,
            request.ConfirmationToken,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Email confirmation failed for {UserId}: {Error}", request.UserId, result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        _logger.LogInformation("Email confirmed successfully for user: {UserId}", request.UserId);
        return Ok(ApiResponse<object>.Ok(new { }, "Email confirmed successfully"));
    }
}
