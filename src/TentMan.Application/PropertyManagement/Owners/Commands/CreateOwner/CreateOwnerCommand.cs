using TentMan.Contracts.Owners;
using MediatR;

namespace TentMan.Application.PropertyManagement.Owners.Commands.CreateOwner;

public record CreateOwnerCommand(
    Guid OrgId,
    TentMan.Domain.Enums.OwnerType OwnerType,
    string DisplayName,
    string Phone,
    string Email,
    string? Pan,
    string? Gstin,
    Guid? LinkedUserId
) : IRequest<OwnerDto>;
