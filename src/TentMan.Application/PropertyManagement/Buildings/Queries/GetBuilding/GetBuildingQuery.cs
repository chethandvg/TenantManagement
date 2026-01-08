using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuilding;

public record GetBuildingQuery(Guid BuildingId) : IRequest<BuildingDetailDto?>;
