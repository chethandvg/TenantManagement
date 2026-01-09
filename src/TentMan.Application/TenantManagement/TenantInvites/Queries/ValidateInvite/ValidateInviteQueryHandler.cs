using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using TentMan.Contracts.TenantInvites;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.ValidateInvite;

public class ValidateInviteQueryHandler : BaseCommandHandler, IRequestHandler<ValidateInviteQuery, ValidateInviteResponse>
{
    private readonly ITenantInviteRepository _inviteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ValidateInviteQueryHandler(
        ITenantInviteRepository inviteRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ValidateInviteQueryHandler> logger)
        : base(currentUser, logger)
    {
        _inviteRepository = inviteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidateInviteResponse> Handle(ValidateInviteQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Validating invite token: {Token}", request.InviteToken);

        var invite = await _inviteRepository.GetByTokenAsync(request.InviteToken, cancellationToken);
        
        if (invite == null)
        {
            return new ValidateInviteResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invite token"
            };
        }

        if (invite.IsUsed)
        {
            return new ValidateInviteResponse
            {
                IsValid = false,
                ErrorMessage = "This invite has already been used"
            };
        }

        if (invite.ExpiresAtUtc < DateTime.UtcNow)
        {
            return new ValidateInviteResponse
            {
                IsValid = false,
                ErrorMessage = "This invite has expired"
            };
        }

        // Get tenant details
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(invite.TenantId, cancellationToken);
        
        if (tenant == null)
        {
            Logger.LogWarning("Tenant {TenantId} not found for valid invite {InviteId}", invite.TenantId, invite.Id);
            return new ValidateInviteResponse
            {
                IsValid = false,
                ErrorMessage = "Invite data is incomplete. Please contact support."
            };
        }

        return new ValidateInviteResponse
        {
            IsValid = true,
            TenantFullName = tenant.FullName,
            Phone = invite.Phone,
            Email = invite.Email
        };
    }
}
