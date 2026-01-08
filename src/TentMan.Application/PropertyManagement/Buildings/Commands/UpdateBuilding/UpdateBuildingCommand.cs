using TentMan.Contracts.Buildings;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.UpdateBuilding;

public record UpdateBuildingCommand(
    Guid BuildingId,
    string Name,
    PropertyType PropertyType,
    int TotalFloors,
    bool HasLift,
    string? Notes,
    byte[] RowVersion
) : IRequest<BuildingDetailDto>;
