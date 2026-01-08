using TentMan.Contracts.Admin;
using MediatR;

namespace TentMan.Application.Admin.Queries.GetRoles;

/// <summary>
/// Query to retrieve all roles in the system.
/// </summary>
public record GetRolesQuery : IRequest<IEnumerable<RoleDto>>;
