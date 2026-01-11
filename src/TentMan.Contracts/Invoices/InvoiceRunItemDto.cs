namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoice run items.
/// </summary>
public sealed class InvoiceRunItemDto
{
    public Guid Id { get; init; }
    public Guid InvoiceRunId { get; init; }
    public Guid LeaseId { get; init; }
    public string LeaseNumber { get; init; } = string.Empty;
    public Guid? InvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
