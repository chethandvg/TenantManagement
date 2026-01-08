using TentMan.Contracts.Buildings;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;

public record CreateBuildingCommand(
    Guid OrgId,
    string BuildingCode,
    string Name,
    PropertyType PropertyType,
    int TotalFloors,
    bool HasLift,
    string? Notes
) : IRequest<BuildingDetailDto>;
