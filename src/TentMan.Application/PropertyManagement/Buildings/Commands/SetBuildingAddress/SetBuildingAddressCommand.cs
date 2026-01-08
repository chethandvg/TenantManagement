using TentMan.Contracts.Buildings;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingAddress;

public record SetBuildingAddressCommand(
    Guid BuildingId,
    string Line1,
    string? Line2,
    string Locality,
    string City,
    string District,
    string State,
    string PostalCode,
    string? Landmark,
    decimal? Latitude,
    decimal? Longitude
) : IRequest<BuildingDetailDto>;
