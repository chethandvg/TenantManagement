using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for lease billing settings.
/// </summary>
public sealed class LeaseBillingSettingDto
{
    public Guid Id { get; init; }
    public Guid LeaseId { get; init; }
    public byte BillingDay { get; init; }
    public short PaymentTermDays { get; init; }
    public bool GenerateInvoiceAutomatically { get; init; }
    public ProrationMethod ProrationMethod { get; init; }
    public string? InvoicePrefix { get; init; }
    public string? PaymentInstructions { get; init; }
    public string? Notes { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
