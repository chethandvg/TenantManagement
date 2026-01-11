using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for lease recurring charges.
/// </summary>
public sealed class LeaseRecurringChargeDto
{
    public Guid Id { get; init; }
    public Guid LeaseId { get; init; }
    public Guid ChargeTypeId { get; init; }
    public string ChargeTypeName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public BillingFrequency Frequency { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
