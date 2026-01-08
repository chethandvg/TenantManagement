using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to add a lease term (financial terms version).
/// </summary>
public class AddLeaseTermRequest
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; } = 0;
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    public EscalationType EscalationType { get; set; } = EscalationType.None;
    public decimal? EscalationValue { get; set; }
    public short? EscalationEveryMonths { get; set; }
    public string? Notes { get; set; }
}
