using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.AddBuildingFile;

public class AddBuildingFileCommandHandler : BaseCommandHandler, IRequestHandler<AddBuildingFileCommand, MediatR.Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddBuildingFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddBuildingFileCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MediatR.Unit> Handle(AddBuildingFileCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding file {FileId} to building: {BuildingId}", request.FileId, request.BuildingId);

        // Validate building exists
        var building = await _unitOfWork.Buildings.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // NOTE: FileMetadata with the given FileId should already exist in the system.
        // Files are uploaded separately before being linked to buildings.
        // In a production system, you would validate file existence through a FileMetadataRepository.
        // For now, this will fail at SaveChanges if FileId doesn't exist due to foreign key constraint.

        var buildingFile = new BuildingFile
        {
            BuildingId = request.BuildingId,
            FileId = request.FileId,
            FileTag = request.FileTag,
            SortOrder = request.SortOrder
        };

        building.BuildingFiles.Add(buildingFile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("File {FileId} added to building: {BuildingId}", request.FileId, request.BuildingId);

        return MediatR.Unit.Value;
    }
}
