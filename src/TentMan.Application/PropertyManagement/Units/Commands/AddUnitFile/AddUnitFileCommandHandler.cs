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

        // Note: Assuming FileMetadata with the given FileId already exists
        // In a real implementation, you would validate this or create it as part of the upload process

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
