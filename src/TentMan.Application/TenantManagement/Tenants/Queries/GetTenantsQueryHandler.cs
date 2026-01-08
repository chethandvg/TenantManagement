using TentMan.Application.Abstractions;
using TentMan.Contracts.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Tenants.Queries;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IEnumerable<TenantListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantsQueryHandler> _logger;

    public GetTenantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantListDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tenants for org: {OrgId}", request.OrgId);

        var tenants = await _unitOfWork.Tenants.GetByOrgIdAsync(request.OrgId, request.Search, cancellationToken);

        return tenants.Select(t => new TenantListDto
        {
            Id = t.Id,
            FullName = t.FullName,
            Phone = t.Phone,
            Email = t.Email,
            IsActive = t.IsActive,
            RowVersion = t.RowVersion
        });
    }
}
