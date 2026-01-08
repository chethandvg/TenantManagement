using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Buildings;

public sealed class AddFileRequest
{
    public Guid FileId { get; init; }
    public FileTag FileTag { get; init; }
    public int SortOrder { get; init; }
}
