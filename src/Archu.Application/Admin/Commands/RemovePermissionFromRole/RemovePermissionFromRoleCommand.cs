using System.Collections.Generic;
using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Commands.RemovePermissionFromRole;

/// <summary>
/// Command that detaches one or more permissions from a role.
/// </summary>
public sealed record RemovePermissionFromRoleCommand(
    Guid RoleId,
    IReadOnlyCollection<string> PermissionNames) : IRequest<RolePermissionsDto>;
