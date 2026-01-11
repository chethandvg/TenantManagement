using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a utility statement/bill for a lease.
/// Supports both amount-based (direct billing) and meter-based (calculated from consumption).
/// </summary>
public class UtilityStatement : BaseEntity
{
    public UtilityStatement()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsMeterBased = false;
        Version = 1;
        IsFinal = false;
    }

    public Guid LeaseId { get; set; }
    public UtilityType UtilityType { get; set; }
    public DateOnly BillingPeriodStart { get; set; }
    public DateOnly BillingPeriodEnd { get; set; }
    public bool IsMeterBased { get; set; }

    // Meter-based fields
    public Guid? UtilityRatePlanId { get; set; }
    public decimal? PreviousReading { get; set; }
    public decimal? CurrentReading { get; set; }
    public decimal? UnitsConsumed { get; set; }
    public decimal? CalculatedAmount { get; set; }

    // Amount-based fields (direct billing from provider)
    public decimal? DirectBillAmount { get; set; }

    // Final amounts
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public Guid? InvoiceLineId { get; set; } // Link to invoice line when billed
    
    // Versioning for multiple corrections/updates
    public int Version { get; set; } // 1, 2, 3, etc. for corrections
    public bool IsFinal { get; set; } // Only one final version allowed per period/utility type

    // Navigation properties
    public Lease Lease { get; set; } = null!;
    public UtilityRatePlan? UtilityRatePlan { get; set; }
    public InvoiceLine? InvoiceLine { get; set; }
}
