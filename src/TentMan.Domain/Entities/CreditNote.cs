using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a credit note issued against an invoice.
/// Used for refunds, adjustments, or corrections.
/// </summary>
public class CreditNote : BaseEntity
{
    public CreditNote()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public DateOnly CreditNoteDate { get; set; }
    public CreditNoteReason Reason { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime? AppliedAtUtc { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
}
