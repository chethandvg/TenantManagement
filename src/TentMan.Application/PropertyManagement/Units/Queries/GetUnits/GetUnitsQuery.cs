using TentMan.Contracts.Units;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Queries.GetUnits;

public record GetUnitsQuery(Guid BuildingId) : IRequest<IEnumerable<UnitListDto>>;
