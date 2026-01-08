using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents financial terms for a lease (versioned, append-only).
/// When terms change, create a new row instead of updating existing one.
/// </summary>
public class LeaseTerm : BaseEntity
{
    public LeaseTerm()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        SecurityDeposit = 0;
        EscalationType = EscalationType.None;
    }

    public Guid LeaseId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; } // Optional fixed monthly
    public decimal? OtherFixedCharge { get; set; }
    public EscalationType EscalationType { get; set; }
    public decimal? EscalationValue { get; set; }
    public short? EscalationEveryMonths { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation property
    public Lease Lease { get; set; } = null!;
}
