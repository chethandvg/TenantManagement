using System.ComponentModel.DataAnnotations;

namespace Archu.Contracts.Authentication;

/// <summary>
/// Request model for user registration.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// User's email address. Must be unique in the system.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's password. Must meet complexity requirements.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// User's username. Will be displayed in the application.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string UserName { get; init; } = string.Empty;
}

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// The refresh token obtained during login or previous token refresh.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Request model for changing password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for verification.
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// The new password. Must meet complexity requirements.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Request model for initiating password reset.
/// </summary>
public sealed record ForgotPasswordRequest
{
    /// <summary>
    /// Email address of the account to reset.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Request model for resetting password with a token.
/// </summary>
public sealed record ResetPasswordRequest
{
    /// <summary>
    /// Email address of the account.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token received via email.
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string ResetToken { get; init; } = string.Empty;

    /// <summary>
    /// New password. Must meet complexity requirements.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Request model for confirming email.
/// </summary>
public sealed record ConfirmEmailRequest
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Email confirmation token received via email.
    /// </summary>
    [Required(ErrorMessage = "Confirmation token is required")]
    public string ConfirmationToken { get; init; } = string.Empty;
}

// ✅ FIX #2 & #3: Removed AuthenticationResponse and UserInfoResponse
// These types conflict with AuthenticationResult and UserInfo in Application layer
// Controllers should use AuthenticationResult from IAuthenticationService
