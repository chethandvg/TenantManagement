using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildingById;

public record GetBuildingByIdQuery(Guid BuildingId) : IRequest<BuildingDetailDto?>;
