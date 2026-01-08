using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;

public record SetUnitOwnershipCommand(
    Guid UnitId,
    List<OwnershipShareRequest> Shares,
    DateTime EffectiveFrom
) : IRequest<MediatR.Unit>;
