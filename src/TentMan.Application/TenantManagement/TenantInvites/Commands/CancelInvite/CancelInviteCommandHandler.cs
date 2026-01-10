using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.CancelInvite;

public class CancelInviteCommandHandler : BaseCommandHandler, IRequestHandler<CancelInviteCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantInviteRepository _inviteRepository;

    public CancelInviteCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantInviteRepository inviteRepository,
        ICurrentUser currentUser,
        ILogger<CancelInviteCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _inviteRepository = inviteRepository;
    }

    public async Task Handle(CancelInviteCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Canceling invite: {InviteId}", request.InviteId);

        // Get the invite
        var invite = await _inviteRepository.GetByIdAsync(request.InviteId, cancellationToken);
        if (invite == null)
        {
            throw new InvalidOperationException($"Invite {request.InviteId} not found");
        }

        // Validate invite belongs to the organization
        if (invite.OrgId != request.OrgId)
        {
            throw new InvalidOperationException($"Invite does not belong to organization {request.OrgId}");
        }

        // Check if invite is already used
        if (invite.IsUsed)
        {
            throw new InvalidOperationException("Cannot cancel an invite that has already been used");
        }

        // Soft delete the invite
        var originalRowVersion = invite.RowVersion;
        invite.IsDeleted = true;
        invite.ModifiedAtUtc = DateTime.UtcNow;
        invite.ModifiedBy = CurrentUser.UserId ?? "System";

        await _inviteRepository.UpdateAsync(invite, originalRowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Invite {InviteId} canceled successfully", request.InviteId);
    }
}
