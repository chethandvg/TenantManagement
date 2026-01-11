using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// Line item in a credit note creation request.
/// </summary>
public sealed class CreateCreditNoteLineRequest
{
    [Required]
    public Guid InvoiceLineId { get; init; }
    
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
    
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
    
    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Request to create a credit note.
/// </summary>
public sealed class CreateCreditNoteRequest
{
    [Required]
    public Guid InvoiceId { get; init; }
    
    [Required]
    public DateOnly CreditNoteDate { get; init; }
    
    [Required]
    public CreditNoteReason Reason { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    [Required]
    [MinLength(1)]
    public IEnumerable<CreateCreditNoteLineRequest> Lines { get; init; } = new List<CreateCreditNoteLineRequest>();
}
