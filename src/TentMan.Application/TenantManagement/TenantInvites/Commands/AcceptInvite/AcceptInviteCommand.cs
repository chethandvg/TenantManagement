using TentMan.Contracts.Authentication;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.AcceptInvite;

public record AcceptInviteCommand(
    string InviteToken,
    string UserName,
    string Email,
    string Password
) : IRequest<AuthenticationResponse>;
