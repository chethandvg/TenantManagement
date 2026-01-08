using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingAddress;

public class SetBuildingAddressCommandHandler : BaseCommandHandler, IRequestHandler<SetBuildingAddressCommand, MediatR.Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetBuildingAddressCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SetBuildingAddressCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MediatR.Unit> Handle(SetBuildingAddressCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting address for building: {BuildingId}", request.BuildingId);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);
        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        if (building.Address == null)
        {
            // Create new address
            building.Address = new BuildingAddress
            {
                BuildingId = request.BuildingId,
                Line1 = request.Line1,
                Line2 = request.Line2,
                Locality = request.Locality,
                City = request.City,
                District = request.District,
                State = request.State,
                PostalCode = request.PostalCode,
                Landmark = request.Landmark,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };
        }
        else
        {
            // Update existing address
            building.Address.Line1 = request.Line1;
            building.Address.Line2 = request.Line2;
            building.Address.Locality = request.Locality;
            building.Address.City = request.City;
            building.Address.District = request.District;
            building.Address.State = request.State;
            building.Address.PostalCode = request.PostalCode;
            building.Address.Landmark = request.Landmark;
            building.Address.Latitude = request.Latitude;
            building.Address.Longitude = request.Longitude;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Address set for building: {BuildingId}", request.BuildingId);

        return MediatR.Unit.Value;
    }
}
