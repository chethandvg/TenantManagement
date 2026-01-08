using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Admin;

/// <summary>
/// Request to create a new role in the system.
/// </summary>
public sealed class CreateRoleRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }
}
