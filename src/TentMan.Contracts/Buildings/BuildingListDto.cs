using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Buildings;

public sealed class BuildingListDto
{
    public Guid Id { get; init; }
    public string BuildingCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public int TotalFloors { get; init; }
    public bool HasLift { get; init; }
    public int UnitCount { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
