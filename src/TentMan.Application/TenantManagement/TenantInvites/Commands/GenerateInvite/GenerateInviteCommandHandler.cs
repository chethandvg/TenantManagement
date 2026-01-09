using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using TentMan.Contracts.TenantInvites;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.GenerateInvite;

public class GenerateInviteCommandHandler : BaseCommandHandler, IRequestHandler<GenerateInviteCommand, TenantInviteDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantInviteRepository _inviteRepository;

    public GenerateInviteCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantInviteRepository inviteRepository,
        ICurrentUser currentUser,
        ILogger<GenerateInviteCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _inviteRepository = inviteRepository;
    }

    public async Task<TenantInviteDto> Handle(GenerateInviteCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Generating invite for tenant: {TenantId}", request.TenantId);

        // Validate tenant exists
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} not found");
        }

        // Validate tenant belongs to the organization
        if (tenant.OrgId != request.OrgId)
        {
            throw new InvalidOperationException($"Tenant does not belong to organization {request.OrgId}");
        }

        // Generate unique token
        var token = GenerateToken();

        var invite = new TenantInvite
        {
            OrgId = request.OrgId,
            TenantId = request.TenantId,
            InviteToken = token,
            Phone = tenant.Phone,
            Email = tenant.Email,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(request.ExpiryDays),
            IsUsed = false
        };

        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Invite generated with token: {Token}", token);

        return new TenantInviteDto
        {
            Id = invite.Id,
            OrgId = invite.OrgId,
            TenantId = invite.TenantId,
            InviteToken = invite.InviteToken,
            InviteUrl = $"/accept-invite?token={invite.InviteToken}",
            Phone = invite.Phone,
            Email = invite.Email,
            ExpiresAtUtc = invite.ExpiresAtUtc,
            IsUsed = invite.IsUsed,
            UsedAtUtc = invite.UsedAtUtc,
            TenantFullName = tenant.FullName
        };
    }

    private static string GenerateToken()
    {
        // Generate cryptographically secure random token (32 characters hex)
        var bytes = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
