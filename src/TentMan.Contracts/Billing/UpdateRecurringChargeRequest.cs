using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to update a recurring charge for a lease.
/// Uses optimistic concurrency control via RowVersion to prevent conflicting updates.
/// Note: ChargeTypeId cannot be changed after creation - create a new charge instead.
/// </summary>
public sealed class UpdateRecurringChargeRequest
{
    /// <summary>
    /// Gets the updated description of this recurring charge.
    /// Should clearly identify what this charge is for (e.g., "Monthly Rent - Unit A-101").
    /// Maximum length is 500 characters.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the updated charge amount per billing cycle.
    /// Must be greater than 0. Will be prorated if charge starts or ends mid-period.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Gets the updated billing frequency for this charge.
    /// OneTime: Charged once on the start date.
    /// Monthly: Charged every month.
    /// Quarterly: Charged every 3 months.
    /// Yearly: Charged once per year.
    /// </summary>
    [Required]
    public BillingFrequency Frequency { get; init; }
    
    /// <summary>
    /// Gets the updated date when this recurring charge becomes effective.
    /// The charge will be included in invoices for billing periods on or after this date.
    /// </summary>
    [Required]
    public DateOnly StartDate { get; init; }
    
    /// <summary>
    /// Gets the optional updated end date when this recurring charge stops.
    /// If null, the charge continues indefinitely until manually deactivated.
    /// Must be after the start date if provided.
    /// </summary>
    public DateOnly? EndDate { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this charge is currently active.
    /// Inactive charges are not included in invoice generation.
    /// Use this to temporarily disable a charge without deleting it.
    /// </summary>
    public bool IsActive { get; init; }
    
    /// <summary>
    /// Gets optional internal notes about this recurring charge.
    /// Not displayed to tenants. Maximum length is 2000 characters.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// Must match the current row version in the database or the update will fail.
    /// </summary>
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
