using TentMan.Domain.Enums;

namespace TentMan.Contracts.Buildings;

public sealed class UpdateBuildingRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public int TotalFloors { get; init; }
    public bool HasLift { get; init; }
    public string? Notes { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
