using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for lease billing settings that control invoice generation and billing behavior.
/// </summary>
public sealed class LeaseBillingSettingDto
{
    /// <summary>
    /// Gets the unique identifier for the billing setting.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the lease ID this billing setting is associated with.
    /// </summary>
    public Guid LeaseId { get; init; }
    
    /// <summary>
    /// Gets the day of the month (1-28) when invoices should be generated.
    /// Restricted to 1-28 to ensure compatibility with all months including February.
    /// </summary>
    public byte BillingDay { get; init; }
    
    /// <summary>
    /// Gets the number of days after invoice date when payment is due.
    /// For example, if set to 5, payment is due 5 days after the invoice date.
    /// </summary>
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
    public ProrationMethod ProrationMethod { get; init; }
    
    /// <summary>
    /// Gets the custom prefix for invoice numbers (e.g., "INV", "BILL").
    /// Used in generating invoice numbers with format: {Prefix}-{YYYYMM}-{Sequence}.
    /// </summary>
    public string? InvoicePrefix { get; init; }
    
    /// <summary>
    /// Gets custom payment instructions to display on all invoices for this lease.
    /// Example: "Please pay via bank transfer to Account #12345".
    /// </summary>
    public string? PaymentInstructions { get; init; }
    
    /// <summary>
    /// Gets internal notes about this billing configuration.
    /// Not displayed to tenants.
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// Used to prevent conflicting updates from multiple users.
    /// </summary>
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
