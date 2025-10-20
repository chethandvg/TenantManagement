using Archu.Application.Abstractions.Authentication;
using Archu.Contracts.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.Api.Controllers;

/// <summary>
/// Controller for authentication operations (login, register, refresh token, etc.).
/// </summary>
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
    /// <param name="request">Registration details (email, password, username).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication tokens if successful.</returns>
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
    /// <param name="request">Login credentials (email and password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication tokens if successful.</returns>
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
    /// <param name="request">Refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication tokens if successful.</returns>
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
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
    /// <param name="request">Current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
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
    /// <param name="request">User's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response (always returns success to prevent email enumeration).</returns>
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
    /// <param name="request">Reset token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
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
    /// <param name="request">User ID and confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success response.</returns>
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

// ============================================
// Request DTOs
// ============================================

/// <summary>
/// Request model for user registration.
/// </summary>
public sealed record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Request model for changing password.
/// </summary>
public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Request model for initiating password reset.
/// </summary>
public sealed record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Request model for resetting password with a token.
/// </summary>
public sealed record ResetPasswordRequest
{
    public string Email { get; init; } = string.Empty;
    public string ResetToken { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Request model for confirming email.
/// </summary>
public sealed record ConfirmEmailRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ConfirmationToken { get; init; } = string.Empty;
}
