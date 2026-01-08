using TentMan.Contracts.Admin;
using MediatR;

namespace TentMan.Application.Admin.Commands.CreateRole;

/// <summary>
/// Command to create a new role in the system.
/// </summary>
public record CreateRoleCommand(
    string Name,
    string? Description
) : IRequest<RoleDto>;
