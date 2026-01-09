using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Commands.AddLeaseParty;

public class AddLeasePartyCommandHandler : BaseCommandHandler, IRequestHandler<AddLeasePartyCommand, LeaseDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddLeasePartyCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddLeasePartyCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaseDetailDto> Handle(AddLeasePartyCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding party {TenantId} to lease {LeaseId}", request.TenantId, request.LeaseId);

        var lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {request.LeaseId} not found");

        // Validate tenant exists
        if (!await _unitOfWork.Tenants.ExistsAsync(request.TenantId, cancellationToken))
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} not found");
        }

        // Check if tenant is already in the lease
        if (lease.Parties.Any(p => p.TenantId == request.TenantId && !p.IsDeleted))
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} is already a party in this lease");
        }

        var leaseParty = new LeaseParty
        {
            LeaseId = request.LeaseId,
            TenantId = request.TenantId,
            Role = request.Role,
            IsResponsibleForPayment = request.IsResponsibleForPayment,
            MoveInDate = request.MoveInDate
        };

        lease.Parties.Add(leaseParty);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Party added to lease {LeaseId}", request.LeaseId);

        // Reload to get updated data
        lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken);
        return LeaseMapper.ToDetailDto(lease!);
    }
}
