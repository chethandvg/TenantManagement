using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for charge types.
/// </summary>
public sealed class ChargeTypeDto
{
    public Guid Id { get; init; }
    public Guid? OrgId { get; init; }
    public ChargeTypeCode Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystemDefined { get; init; }
    public bool IsTaxable { get; init; }
    public decimal? DefaultAmount { get; init; }
}
