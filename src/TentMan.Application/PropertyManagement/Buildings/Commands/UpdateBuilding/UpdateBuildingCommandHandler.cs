using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandHandler : BaseCommandHandler, IRequestHandler<UpdateBuildingCommand, BuildingDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBuildingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateBuildingCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BuildingDetailDto> Handle(UpdateBuildingCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating building: {BuildingId}", request.Id);

        var building = await _unitOfWork.Buildings.GetByIdAsync(request.Id, cancellationToken);
        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.Id} not found");
        }

        // Update properties (BuildingCode is immutable, so not updating it)
        building.Name = request.Name;
        building.PropertyType = request.PropertyType;
        building.TotalFloors = request.TotalFloors;
        building.HasLift = request.HasLift;
        building.Notes = request.Notes;

        await _unitOfWork.Buildings.UpdateAsync(building, request.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Building updated: {BuildingId}", building.Id);

        return new BuildingDetailDto
        {
            Id = building.Id,
            OrgId = building.OrgId,
            BuildingCode = building.BuildingCode,
            Name = building.Name,
            PropertyType = building.PropertyType,
            TotalFloors = building.TotalFloors,
            HasLift = building.HasLift,
            Notes = building.Notes,
            RowVersion = building.RowVersion
        };
    }
}
