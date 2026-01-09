using TentMan.Contracts.Tenants;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

/// <summary>
/// Query to get all documents for a tenant.
/// </summary>
public record GetTenantDocumentsQuery(Guid TenantId) : IRequest<IEnumerable<TenantDocumentDto>>;
