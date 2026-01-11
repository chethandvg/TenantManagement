using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// Request to issue a credit note.
/// Note: Concurrency control is handled internally by the service.
/// </summary>
public sealed class IssueCreditNoteRequest
{
    // No parameters needed - the service handles concurrency internally
}
