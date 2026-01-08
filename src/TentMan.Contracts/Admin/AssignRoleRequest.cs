using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Admin;

/// <summary>
/// Request to assign a role to a user.
/// </summary>
public sealed class AssignRoleRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public Guid RoleId { get; init; }
}
