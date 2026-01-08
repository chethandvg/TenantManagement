using TentMan.Domain.Enums;

namespace TentMan.Contracts.Units;

public sealed class AddUnitMeterRequest
{
    public UtilityType UtilityType { get; init; }
    public string MeterNumber { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? ConsumerAccount { get; init; }
}
