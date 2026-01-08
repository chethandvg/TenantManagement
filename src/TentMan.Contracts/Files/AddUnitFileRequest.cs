using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Files;

public sealed class AddUnitFileRequest
{
    public Guid FileId { get; init; }
    public FileTag FileTag { get; init; }
    public int SortOrder { get; init; } = 0;
}
