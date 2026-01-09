using TentMan.Contracts.TenantPortal;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

/// <summary>
/// Query to get the move-in handover checklist for a tenant by their user ID.
/// </summary>
public record GetMoveInHandoverQuery(Guid UserId) : IRequest<MoveInHandoverResponse?>;
