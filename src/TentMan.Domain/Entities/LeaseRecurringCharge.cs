using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a recurring charge associated with a lease.
/// Examples: Monthly rent, maintenance fee, parking fee, etc.
/// </summary>
public class LeaseRecurringCharge : BaseEntity
{
    public LeaseRecurringCharge()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsActive = true;
        Frequency = BillingFrequency.Monthly;
    }

    public Guid LeaseId { get; set; }
    public Guid ChargeTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BillingFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; } // Null = no end date
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Lease Lease { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
