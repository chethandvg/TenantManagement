using System.Collections.Generic;
using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetPermissions;

/// <summary>
/// Query that retrieves all permission definitions.
/// </summary>
public sealed record GetPermissionsQuery : IRequest<IEnumerable<PermissionDto>>;
