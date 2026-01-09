using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using TentMan.Contracts.TenantInvites;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.GetInvitesByTenant;

public class GetInvitesByTenantQueryHandler : BaseCommandHandler, IRequestHandler<GetInvitesByTenantQuery, IEnumerable<TenantInviteDto>>
{
    private readonly ITenantInviteRepository _inviteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetInvitesByTenantQueryHandler(
        ITenantInviteRepository inviteRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetInvitesByTenantQueryHandler> logger)
        : base(currentUser, logger)
    {
        _inviteRepository = inviteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TenantInviteDto>> Handle(GetInvitesByTenantQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Fetching invites for tenant: {TenantId}", request.TenantId);

        // Validate tenant exists and belongs to organization
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} not found");
        }

        if (tenant.OrgId != request.OrgId)
        {
            throw new InvalidOperationException($"Tenant does not belong to organization {request.OrgId}");
        }

        var invites = await _inviteRepository.GetAllByTenantIdAsync(request.TenantId, cancellationToken);

        return invites.Select(invite => new TenantInviteDto
        {
            Id = invite.Id,
            OrgId = invite.OrgId,
            TenantId = invite.TenantId,
            InviteToken = invite.InviteToken,
            InviteUrl = string.Empty, // URL will be constructed by API layer
            Phone = invite.Phone,
            Email = invite.Email,
            ExpiresAtUtc = invite.ExpiresAtUtc,
            IsUsed = invite.IsUsed,
            UsedAtUtc = invite.UsedAtUtc,
            TenantFullName = tenant.FullName
        }).ToList();
    }
}
