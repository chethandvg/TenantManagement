using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// Request to void a credit note.
/// </summary>
public sealed class VoidCreditNoteRequest
{
    [Required]
    [MaxLength(500)]
    public string VoidReason { get; init; } = string.Empty;
    
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
