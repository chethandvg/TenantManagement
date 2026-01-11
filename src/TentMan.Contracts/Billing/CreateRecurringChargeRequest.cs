using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to create a recurring charge for a lease.
/// Recurring charges are automatically included in invoice generation based on their frequency and active dates.
/// </summary>
public sealed class CreateRecurringChargeRequest
{
    /// <summary>
    /// Gets the ID of the charge type to use (RENT, MAINT, etc.).
    /// </summary>
    [Required]
    public Guid ChargeTypeId { get; init; }
    
    /// <summary>
    /// Gets the description of this recurring charge.
    /// Should clearly identify what this charge is for (e.g., "Monthly Rent - Unit A-101").
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the charge amount per billing cycle.
    /// Must be greater than 0. Will be prorated if charge starts or ends mid-period.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Gets the billing frequency for this charge.
    /// OneTime: Charged once on the start date.
    /// Monthly: Charged every month.
    /// Quarterly: Charged every 3 months.
    /// Yearly: Charged once per year.
    /// </summary>
    [Required]
    public BillingFrequency Frequency { get; init; }
    
    /// <summary>
    /// Gets the date when this recurring charge becomes effective.
    /// The charge will be included in invoices for billing periods on or after this date.
    /// </summary>
    [Required]
    public DateOnly StartDate { get; init; }
    
    /// <summary>
    /// Gets the optional end date when this recurring charge stops.
    /// If null, the charge continues indefinitely until manually deactivated.
    /// Must be after the start date if provided.
    /// </summary>
    public DateOnly? EndDate { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this charge is currently active.
    /// Inactive charges are not included in invoice generation.
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Gets optional internal notes about this recurring charge.
    /// Not displayed to tenants.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
}
