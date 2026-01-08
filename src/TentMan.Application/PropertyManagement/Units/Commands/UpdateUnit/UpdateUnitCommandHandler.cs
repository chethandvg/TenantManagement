using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Units;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;

public class UpdateUnitCommandHandler : BaseCommandHandler, IRequestHandler<UpdateUnitCommand, UnitDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUnitCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateUnitCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitDetailDto> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating unit: {UnitId}", request.UnitId);

        var unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);

        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        unit.Floor = request.Floor;
        unit.UnitType = request.UnitType;
        unit.AreaSqFt = request.AreaSqFt;
        unit.Bedrooms = request.Bedrooms;
        unit.Bathrooms = request.Bathrooms;
        unit.Furnishing = request.Furnishing;
        unit.ParkingSlots = request.ParkingSlots;
        unit.OccupancyStatus = request.OccupancyStatus;
        unit.ModifiedAtUtc = DateTime.UtcNow;
        unit.ModifiedBy = CurrentUser.UserId ?? "System";

        await _unitOfWork.Units.UpdateAsync(unit, request.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Unit updated: {UnitId}", unit.Id);

        // Determine resolved ownership (unit override or building default)
        var resolvedOwnership = unit.HasUnitOwnershipOverride && unit.OwnershipShares.Any()
            ? unit.OwnershipShares
                .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
                .Select(s => new OwnerShareDto
                {
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner?.DisplayName ?? "",
                    SharePercent = s.SharePercent
                }).ToList()
            : unit.Building?.OwnershipShares
                .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
                .Select(s => new OwnerShareDto
                {
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner?.DisplayName ?? "",
                    SharePercent = s.SharePercent
                }).ToList() ?? new List<OwnerShareDto>();

        return new UnitDetailDto
        {
            Id = unit.Id,
            BuildingId = unit.BuildingId,
            UnitNumber = unit.UnitNumber,
            Floor = unit.Floor,
            UnitType = unit.UnitType,
            AreaSqFt = unit.AreaSqFt,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            Furnishing = unit.Furnishing,
            ParkingSlots = unit.ParkingSlots,
            OccupancyStatus = unit.OccupancyStatus,
            HasUnitOwnershipOverride = unit.HasUnitOwnershipOverride,
            ResolvedOwnership = resolvedOwnership,
            Meters = unit.Meters
                .Where(m => m.IsActive)
                .Select(m => new UnitMeterDto
                {
                    Id = m.Id,
                    UtilityType = m.UtilityType,
                    MeterNumber = m.MeterNumber,
                    Provider = m.Provider,
                    ConsumerAccount = m.ConsumerAccount,
                    IsActive = m.IsActive
                }).ToList(),
            Files = unit.UnitFiles
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
            RowVersion = unit.RowVersion
        };
    }
}
