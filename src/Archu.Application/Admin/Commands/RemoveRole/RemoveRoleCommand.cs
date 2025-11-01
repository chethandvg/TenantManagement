using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Admin.Commands.RemoveRole;

/// <summary>
/// Command to remove a role from a user.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="RoleId">The role's unique identifier.</param>
public record RemoveRoleCommand(
    Guid UserId,
    Guid RoleId
) : IRequest<Result>;
