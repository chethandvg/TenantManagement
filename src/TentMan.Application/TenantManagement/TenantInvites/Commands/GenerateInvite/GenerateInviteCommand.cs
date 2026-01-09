using TentMan.Contracts.TenantInvites;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.GenerateInvite;

public record GenerateInviteCommand(
    Guid OrgId,
    Guid TenantId,
    int ExpiryDays
) : IRequest<TenantInviteDto>;
