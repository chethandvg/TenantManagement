using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;

public class AddUnitFileCommandHandler : BaseCommandHandler, IRequestHandler<AddUnitFileCommand, MediatR.Unit>
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

    public async Task<MediatR.Unit> Handle(AddUnitFileCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding file {FileId} to unit: {UnitId}", request.FileId, request.UnitId);

        // Validate unit exists
        var unit = await _unitOfWork.Units.GetByIdAsync(request.UnitId, cancellationToken);
        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        // NOTE: FileMetadata with the given FileId should already exist in the system.
        // Files are uploaded separately before being linked to units.
        // In a production system, you would validate file existence through a FileMetadataRepository.
        // For now, this will fail at SaveChanges if FileId doesn't exist due to foreign key constraint.

        var unitFile = new UnitFile
        {
            UnitId = request.UnitId,
            FileId = request.FileId,
            FileTag = request.FileTag,
            SortOrder = request.SortOrder
        };

        unit.UnitFiles.Add(unitFile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("File {FileId} added to unit: {UnitId}", request.FileId, request.UnitId);

        return MediatR.Unit.Value;
    }
}
