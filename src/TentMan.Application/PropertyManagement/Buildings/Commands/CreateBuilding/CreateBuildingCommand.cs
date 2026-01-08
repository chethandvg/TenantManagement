using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;

public record CreateBuildingCommand(
    Guid OrgId,
    string BuildingCode,
    string Name,
    TentMan.Domain.Enums.PropertyType PropertyType,
    int TotalFloors,
    bool HasLift,
    string? Notes
) : IRequest<BuildingDetailDto>;
