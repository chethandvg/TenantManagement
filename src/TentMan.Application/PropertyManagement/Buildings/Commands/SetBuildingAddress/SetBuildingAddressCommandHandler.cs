using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingAddress;

public class SetBuildingAddressCommandHandler : BaseCommandHandler, IRequestHandler<SetBuildingAddressCommand, BuildingDetailDto>
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

    public async Task<BuildingDetailDto> Handle(SetBuildingAddressCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting address for building: {BuildingId}", request.BuildingId);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        if (building.Address == null)
        {
            building.Address = new BuildingAddress
            {
                BuildingId = building.Id,
                CreatedBy = CurrentUser.UserId ?? "System"
            };
        }

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
        building.Address.ModifiedAtUtc = DateTime.UtcNow;
        building.Address.ModifiedBy = CurrentUser.UserId ?? "System";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Address set for building: {BuildingId}", building.Id);

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
            Address = new BuildingAddressDto
            {
                Line1 = building.Address.Line1,
                Line2 = building.Address.Line2,
                Locality = building.Address.Locality,
                City = building.Address.City,
                District = building.Address.District,
                State = building.Address.State,
                PostalCode = building.Address.PostalCode,
                Landmark = building.Address.Landmark,
                Latitude = building.Address.Latitude,
                Longitude = building.Address.Longitude
            },
            CurrentOwners = building.OwnershipShares
                .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
                .Select(s => new OwnerShareDto
                {
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner?.DisplayName ?? "",
                    SharePercent = s.SharePercent
                }).ToList(),
            Files = building.BuildingFiles
                .OrderBy(f => f.SortOrder)
                .Select(f => new FileDto
                {
                    FileId = f.FileId,
                    FileName = f.File?.FileName ?? "",
                    FileTag = f.FileTag,
                    SizeBytes = f.File?.SizeBytes ?? 0,
                    ContentType = f.File?.ContentType ?? "",
                    SortOrder = f.SortOrder
                }).ToList(),
            RowVersion = building.RowVersion
        };
    }
}
