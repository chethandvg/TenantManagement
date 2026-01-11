using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// Request to issue an invoice.
/// </summary>
public sealed class IssueInvoiceRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
