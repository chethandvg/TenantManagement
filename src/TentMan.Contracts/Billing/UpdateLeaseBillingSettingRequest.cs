using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to update lease billing settings.
/// Uses optimistic concurrency control via RowVersion to prevent conflicting updates.
/// </summary>
public sealed class UpdateLeaseBillingSettingRequest
{
    /// <summary>
    /// Gets the day of the month (1-28) when invoices should be generated.
    /// Must be between 1 and 28 to ensure compatibility with all months including February.
    /// </summary>
    [Range(1, 28, ErrorMessage = "Billing day must be between 1 and 28")]
    public byte BillingDay { get; init; }
    
    /// <summary>
    /// Gets the number of days after invoice date when payment is due.
    /// Must be between 0 and 365 days. For example, if set to 5, payment is due 5 days after the invoice date.
    /// </summary>
    [Range(0, 365, ErrorMessage = "Payment term days must be between 0 and 365")]
    public short PaymentTermDays { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether invoices should be automatically generated for this lease.
    /// When true, background jobs will automatically create invoices on the billing day.
    /// </summary>
    public bool GenerateInvoiceAutomatically { get; init; }
    
    /// <summary>
    /// Gets the proration calculation method to use when lease starts or ends mid-period.
    /// ActualDaysInMonth uses the actual number of days in the calendar month (28-31).
    /// ThirtyDayMonth uses a fixed 30-day month for consistent calculations.
    /// </summary>
    [Required]
    public ProrationMethod ProrationMethod { get; init; }
    
    /// <summary>
    /// Gets the custom prefix for invoice numbers (e.g., "INV", "BILL").
    /// Used in generating invoice numbers with format: {Prefix}-{YYYYMM}-{Sequence}.
    /// Maximum length is 50 characters.
    /// </summary>
    [MaxLength(50)]
    public string? InvoicePrefix { get; init; }
    
    /// <summary>
    /// Gets custom payment instructions to display on all invoices for this lease.
    /// Example: "Please pay via bank transfer to Account #12345".
    /// Maximum length is 1000 characters.
    /// </summary>
    [MaxLength(1000)]
    public string? PaymentInstructions { get; init; }
    
    /// <summary>
    /// Gets internal notes about this billing configuration.
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
