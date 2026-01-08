using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;

public record SetBuildingOwnershipCommand(
    Guid BuildingId,
    List<OwnershipShareRequest> Shares,
    DateTime EffectiveFrom
) : IRequest<BuildingDetailDto>;
