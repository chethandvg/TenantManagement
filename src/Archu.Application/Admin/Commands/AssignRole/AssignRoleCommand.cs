using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Admin.Commands.AssignRole;

/// <summary>
/// Command to assign a role to a user.
/// </summary>
public record AssignRoleCommand(
    Guid UserId,
    Guid RoleId
) : IRequest<Result>;
