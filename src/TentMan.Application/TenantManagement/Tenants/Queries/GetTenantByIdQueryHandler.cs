using TentMan.Application.Abstractions;
using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Tenants.Queries;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantByIdQueryHandler> _logger;

    public GetTenantByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TenantDetailDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tenant: {TenantId}", request.TenantId);

        var tenant = await _unitOfWork.Tenants.GetByIdWithDetailsAsync(request.TenantId, cancellationToken);

        if (tenant == null)
            return null;

        return TenantMapper.ToDetailDto(tenant);
    }
}
