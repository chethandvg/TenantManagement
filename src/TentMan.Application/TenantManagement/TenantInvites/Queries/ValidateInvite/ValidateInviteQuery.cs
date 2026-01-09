using TentMan.Contracts.TenantInvites;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.ValidateInvite;

public record ValidateInviteQuery(
    string InviteToken
) : IRequest<ValidateInviteResponse>;
