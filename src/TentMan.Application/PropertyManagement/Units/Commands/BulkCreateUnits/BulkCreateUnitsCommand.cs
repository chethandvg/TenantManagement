using TentMan.Contracts.Units;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;

public record BulkCreateUnitsCommand(
    Guid BuildingId,
    List<CreateUnitRequest> Units
) : IRequest<List<UnitListDto>>;
