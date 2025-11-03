using System.Collections.Generic;
using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Commands.RemovePermissionFromUser;

/// <summary>
/// Command that revokes direct permissions from a user.
/// </summary>
public sealed record RemovePermissionFromUserCommand(
    Guid UserId,
    IReadOnlyCollection<string> PermissionNames) : IRequest<UserPermissionsDto>;
