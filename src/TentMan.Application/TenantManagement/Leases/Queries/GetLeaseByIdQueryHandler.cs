using TentMan.Application.Abstractions;
using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Leases;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Queries;

public class GetLeaseByIdQueryHandler : IRequestHandler<GetLeaseByIdQuery, LeaseDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetLeaseByIdQueryHandler> _logger;

    public GetLeaseByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetLeaseByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LeaseDetailDto?> Handle(GetLeaseByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting lease: {LeaseId}", request.LeaseId);

        var lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken);

        if (lease == null)
            return null;

        return LeaseMapper.ToDetailDto(lease);
    }
}
