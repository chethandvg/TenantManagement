using TentMan.Contracts.Units;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;

public record AddUnitFileCommand(
    Guid UnitId,
    Guid FileId,
    FileTag FileTag,
    int SortOrder
) : IRequest<UnitDetailDto>;
