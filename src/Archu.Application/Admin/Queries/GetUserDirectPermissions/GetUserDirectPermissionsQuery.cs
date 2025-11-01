using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetUserDirectPermissions;

/// <summary>
/// Query for retrieving permissions assigned directly to a user.
/// </summary>
public sealed record GetUserDirectPermissionsQuery(Guid UserId) : IRequest<UserPermissionsDto>;
