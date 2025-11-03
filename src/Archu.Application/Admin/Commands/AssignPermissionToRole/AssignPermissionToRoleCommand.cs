using System.Collections.Generic;
using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Commands.AssignPermissionToRole;

/// <summary>
/// Command that links one or more permissions to a role.
/// </summary>
public sealed record AssignPermissionToRoleCommand(
    Guid RoleId,
    IReadOnlyCollection<string> PermissionNames) : IRequest<RolePermissionsDto>;
