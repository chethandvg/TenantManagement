using TentMan.Application.Abstractions;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Queries;

public class GetLeasesByUnitQueryHandler : IRequestHandler<GetLeasesByUnitQuery, IEnumerable<LeaseListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetLeasesByUnitQueryHandler> _logger;

    public GetLeasesByUnitQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetLeasesByUnitQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<LeaseListDto>> Handle(GetLeasesByUnitQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leases for unit: {UnitId}", request.UnitId);

        var leases = await _unitOfWork.Leases.GetByUnitIdAsync(request.UnitId, cancellationToken);

        return leases.Select(l =>
        {
            var primaryTenant = l.Parties.FirstOrDefault(p => !p.IsDeleted && p.Role == LeasePartyRole.PrimaryTenant);
            var activeTerm = l.Terms
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.EffectiveFrom)
                .FirstOrDefault(t => t.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) && 
                                     (t.EffectiveTo == null || t.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)));

            return new LeaseListDto
            {
                Id = l.Id,
                UnitId = l.UnitId,
                UnitNumber = l.Unit?.UnitNumber,
                BuildingName = l.Unit?.Building?.Name,
                LeaseNumber = l.LeaseNumber,
                Status = l.Status,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                PrimaryTenantName = primaryTenant?.Tenant?.FullName,
                CurrentRent = activeTerm?.MonthlyRent,
                RowVersion = l.RowVersion
            };
        });
    }
}
