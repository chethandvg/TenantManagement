using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// Request to issue a credit note.
/// </summary>
public sealed class IssueCreditNoteRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
