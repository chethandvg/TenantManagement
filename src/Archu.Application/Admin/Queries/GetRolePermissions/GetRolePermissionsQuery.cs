using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetRolePermissions;

/// <summary>
/// Query for retrieving permissions assigned to a specific role.
/// </summary>
public sealed record GetRolePermissionsQuery(Guid RoleId) : IRequest<RolePermissionsDto>;
