using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildings;

public record GetBuildingsQuery(Guid OrgId) : IRequest<IEnumerable<BuildingListDto>>;
