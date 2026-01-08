namespace TentMan.Contracts.Units;

public sealed class BulkCreateUnitsRequest
{
    public Guid BuildingId { get; init; }
    public List<CreateUnitRequest> Units { get; init; } = new();
}
