using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;

public record AddUnitFileCommand(
    Guid UnitId,
    Guid FileId,
    FileTag FileTag,
    int SortOrder = 0
) : IRequest<MediatR.Unit>;
