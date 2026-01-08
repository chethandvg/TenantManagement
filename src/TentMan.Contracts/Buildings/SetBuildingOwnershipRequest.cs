namespace TentMan.Contracts.Buildings;

public sealed class SetBuildingOwnershipRequest
{
    public List<OwnershipShareRequest> Shares { get; init; } = new();
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
}

public sealed class OwnershipShareRequest
{
    public Guid OwnerId { get; init; }
    public decimal SharePercent { get; init; }
}
