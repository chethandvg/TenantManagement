using TentMan.Contracts.Enums;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// DTO for credit notes.
/// </summary>
public sealed class CreditNoteDto
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public Guid InvoiceId { get; init; }
    public string CreditNoteNumber { get; init; } = string.Empty;
    public DateOnly CreditNoteDate { get; init; }
    public CreditNoteReason Reason { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public DateTime? AppliedAtUtc { get; init; }
    public IEnumerable<CreditNoteLineDto> Lines { get; init; } = new List<CreditNoteLineDto>();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
