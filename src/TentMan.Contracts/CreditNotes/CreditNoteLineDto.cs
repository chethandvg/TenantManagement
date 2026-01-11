namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// DTO for credit note line items.
/// </summary>
public sealed class CreditNoteLineDto
{
    public Guid Id { get; init; }
    public Guid CreditNoteId { get; init; }
    public Guid InvoiceLineId { get; init; }
    public int LineNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Notes { get; init; }
}
