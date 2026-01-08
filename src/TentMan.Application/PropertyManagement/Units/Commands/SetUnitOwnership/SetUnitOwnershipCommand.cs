using TentMan.Contracts.Buildings;
using TentMan.Contracts.Units;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;

public record SetUnitOwnershipCommand(
    Guid UnitId,
    List<OwnershipShareRequest> Shares,
    DateTime EffectiveFrom
) : IRequest<UnitDetailDto>;
