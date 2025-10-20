using System.ComponentModel.DataAnnotations;

namespace Archu.Contracts.Authentication;

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Gets the username or email.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets the password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether to remember the user.
    /// </summary>
    public bool RememberMe { get; init; }
}
