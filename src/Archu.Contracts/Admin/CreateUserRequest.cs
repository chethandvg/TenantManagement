using System.ComponentModel.DataAnnotations;

namespace Archu.Contracts.Admin;

/// <summary>
/// Request to create a new user in the system.
/// </summary>
public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? PhoneNumber { get; init; }

    public bool EmailConfirmed { get; init; } = false;

    public bool TwoFactorEnabled { get; init; } = false;
}
