using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;

public class CreateBuildingCommandHandler : BaseCommandHandler, IRequestHandler<CreateBuildingCommand, BuildingDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBuildingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateBuildingCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BuildingDetailDto> Handle(CreateBuildingCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating building: {Name} for organization: {OrgId}", request.Name, request.OrgId);

        // Validate organization exists
        if (!await _unitOfWork.Organizations.ExistsAsync(request.OrgId, cancellationToken))
        {
            throw new InvalidOperationException($"Organization {request.OrgId} not found");
        }

        // Check for duplicate building code
        if (await _unitOfWork.Buildings.BuildingCodeExistsAsync(request.OrgId, request.BuildingCode, null, cancellationToken))
        {
            throw new InvalidOperationException($"Building code '{request.BuildingCode}' already exists in this organization");
        }

        var building = new Building
        {
            OrgId = request.OrgId,
            BuildingCode = request.BuildingCode,
            Name = request.Name,
            PropertyType = request.PropertyType,
            TotalFloors = request.TotalFloors,
            HasLift = request.HasLift,
            Notes = request.Notes
        };

        await _unitOfWork.Buildings.AddAsync(building, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Building created with ID: {BuildingId}", building.Id);

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
