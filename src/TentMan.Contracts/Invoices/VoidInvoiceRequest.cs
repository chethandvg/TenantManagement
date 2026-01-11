using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// Request to void an invoice.
/// </summary>
public sealed class VoidInvoiceRequest
{
    [Required]
    [MaxLength(500)]
    public string VoidReason { get; init; } = string.Empty;
    
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
