using TentMan.Contracts.Buildings;

namespace TentMan.Contracts.Units;

public sealed class SetUnitOwnershipRequest
{
    public List<OwnershipShareRequest> Shares { get; init; } = new();
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
}
