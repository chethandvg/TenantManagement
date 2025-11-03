using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetEffectiveUserPermissions;

/// <summary>
/// Query that computes the effective permissions for a user including role assignments.
/// </summary>
public sealed record GetEffectiveUserPermissionsQuery(Guid UserId) : IRequest<EffectiveUserPermissionsDto>;
