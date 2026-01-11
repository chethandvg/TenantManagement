using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for lease recurring charges that are automatically included in invoice generation.
/// Represents charges like rent, maintenance fees, parking, etc. that recur on a regular schedule.
/// </summary>
public sealed class LeaseRecurringChargeDto
{
    /// <summary>
    /// Gets the unique identifier for the recurring charge.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the lease ID this recurring charge is associated with.
    /// </summary>
    public Guid LeaseId { get; init; }
    
    /// <summary>
    /// Gets the ID of the charge type (RENT, MAINT, etc.).
    /// </summary>
    public Guid ChargeTypeId { get; init; }
    
    /// <summary>
    /// Gets the display name of the charge type for UI presentation.
    /// </summary>
    public string ChargeTypeName { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the description of this recurring charge.
    /// Should clearly identify what this charge is for (e.g., "Monthly Rent - Unit A-101").
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the charge amount per billing cycle.
    /// Will be prorated if charge starts or ends mid-period.
    /// </summary>
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Gets the billing frequency for this charge.
    /// OneTime: Charged once on the start date.
    /// Monthly: Charged every month.
    /// Quarterly: Charged every 3 months.
    /// Yearly: Charged once per year.
    /// </summary>
    public BillingFrequency Frequency { get; init; }
    
    /// <summary>
    /// Gets the date when this recurring charge becomes effective.
    /// The charge will be included in invoices for billing periods on or after this date.
    /// </summary>
    public DateOnly StartDate { get; init; }
    
    /// <summary>
    /// Gets the optional end date when this recurring charge stops.
    /// If null, the charge continues indefinitely until manually deactivated.
    /// </summary>
    public DateOnly? EndDate { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this charge is currently active.
    /// Inactive charges are not included in invoice generation.
    /// </summary>
    public bool IsActive { get; init; }
    
    /// <summary>
    /// Gets optional internal notes about this recurring charge.
    /// Not displayed to tenants.
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
