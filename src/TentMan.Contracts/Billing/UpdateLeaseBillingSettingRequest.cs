using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to update lease billing settings.
/// </summary>
public sealed class UpdateLeaseBillingSettingRequest
{
    [Range(1, 28, ErrorMessage = "Billing day must be between 1 and 28")]
    public byte BillingDay { get; init; }
    
    [Range(0, 365, ErrorMessage = "Payment term days must be between 0 and 365")]
    public short PaymentTermDays { get; init; }
    
    public bool GenerateInvoiceAutomatically { get; init; }
    
    [MaxLength(50)]
    public string? InvoicePrefix { get; init; }
    
    [MaxLength(1000)]
    public string? PaymentInstructions { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
