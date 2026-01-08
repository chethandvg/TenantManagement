using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Units;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;

public class AddUnitFileCommandHandler : BaseCommandHandler, IRequestHandler<AddUnitFileCommand, UnitDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddUnitFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddUnitFileCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitDetailDto> Handle(AddUnitFileCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding file {FileId} to unit: {UnitId}", request.FileId, request.UnitId);

        var unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);

        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        // Check if file is already linked
        if (unit.UnitFiles.Any(f => f.FileId == request.FileId))
        {
            throw new InvalidOperationException($"File {request.FileId} is already linked to this unit");
        }

        var unitFile = new UnitFile
        {
            UnitId = unit.Id,
            FileId = request.FileId,
            FileTag = request.FileTag,
            SortOrder = request.SortOrder,
            CreatedBy = CurrentUser.UserId ?? "System"
        };

        unit.UnitFiles.Add(unitFile);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get file details
        unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);

        Logger.LogInformation("File {FileId} added to unit: {UnitId}", request.FileId, unit!.Id);

        // Determine resolved ownership
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
