using MediatR;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.CancelInvite;

public record CancelInviteCommand(
    Guid OrgId,
    Guid InviteId
) : IRequest;
