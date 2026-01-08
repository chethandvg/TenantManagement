using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.AddBuildingFile;

public record AddBuildingFileCommand(
    Guid BuildingId,
    Guid FileId,
    FileTag FileTag,
    int SortOrder = 0
) : IRequest<MediatR.Unit>;
