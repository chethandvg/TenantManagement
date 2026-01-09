using TentMan.Contracts.Tenants;
using MediatR;

namespace TentMan.Application.TenantManagement.Tenants.Queries;

public record GetTenantByIdQuery(Guid TenantId) : IRequest<TenantDetailDto?>;
