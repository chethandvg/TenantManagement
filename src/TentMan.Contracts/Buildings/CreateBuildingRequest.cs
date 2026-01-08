using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Buildings;

public sealed class CreateBuildingRequest
{
    public Guid OrgId { get; init; }
    public string BuildingCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public int TotalFloors { get; init; }
    public bool HasLift { get; init; }
    public string? Notes { get; init; }
}
