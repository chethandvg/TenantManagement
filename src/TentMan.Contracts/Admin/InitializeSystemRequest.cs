using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Admin;

/// <summary>
/// Request to initialize the system with a super admin user.
/// This should only be used once during initial system setup.
/// </summary>
public sealed class InitializeSystemRequest
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; init; } = "superadmin";

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;
}
