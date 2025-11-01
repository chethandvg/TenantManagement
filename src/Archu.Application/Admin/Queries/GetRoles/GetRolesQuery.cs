using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetRoles;

/// <summary>
/// Query to retrieve all roles in the system.
/// </summary>
public record GetRolesQuery : IRequest<IEnumerable<RoleDto>>;
