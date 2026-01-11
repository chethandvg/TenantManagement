using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoices.
/// </summary>
public sealed class InvoiceDto
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public Guid LeaseId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly DueDate { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateOnly BillingPeriodStart { get; init; }
    public DateOnly BillingPeriodEnd { get; init; }
    
    // Amounts
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal BalanceAmount { get; init; }
    
    // Dates
    public DateTime? IssuedAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }
    public DateTime? VoidedAtUtc { get; init; }
    
    // Additional info
    public string? Notes { get; init; }
    public string? PaymentInstructions { get; init; }
    public string? VoidReason { get; init; }
    
    public IEnumerable<InvoiceLineDto> Lines { get; init; } = new List<InvoiceLineDto>();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
