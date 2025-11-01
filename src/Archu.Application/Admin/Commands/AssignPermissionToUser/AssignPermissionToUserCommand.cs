using System.Collections.Generic;
using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Commands.AssignPermissionToUser;

/// <summary>
/// Command that grants permissions directly to a user.
/// </summary>
public sealed record AssignPermissionToUserCommand(
    Guid UserId,
    IReadOnlyCollection<string> PermissionNames) : IRequest<UserPermissionsDto>;
