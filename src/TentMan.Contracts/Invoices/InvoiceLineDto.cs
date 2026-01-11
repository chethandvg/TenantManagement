using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoice line items.
/// </summary>
public sealed class InvoiceLineDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid ChargeTypeId { get; init; }
    public string ChargeTypeName { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public string? Source { get; init; }
    public Guid? SourceRefId { get; init; }
}
