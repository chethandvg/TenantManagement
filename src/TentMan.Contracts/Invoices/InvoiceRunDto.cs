using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoice runs.
/// </summary>
public sealed class InvoiceRunDto
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public string RunNumber { get; init; } = string.Empty;
    public DateOnly BillingPeriodStart { get; init; }
    public DateOnly BillingPeriodEnd { get; init; }
    public InvoiceRunStatus Status { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public int TotalLeases { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Notes { get; init; }
    public IEnumerable<InvoiceRunItemDto> Items { get; init; } = new List<InvoiceRunItemDto>();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
