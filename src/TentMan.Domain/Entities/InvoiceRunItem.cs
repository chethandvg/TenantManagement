using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an individual item (lease) in an invoice run.
/// Tracks the processing status and result for each lease.
/// </summary>
public class InvoiceRunItem : BaseEntity
{
    public InvoiceRunItem()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsSuccess = false;
    }

    public Guid InvoiceRunId { get; set; }
    public Guid LeaseId { get; set; }
    public Guid? InvoiceId { get; set; } // Null if failed to generate
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }

    // Navigation properties
    public InvoiceRun InvoiceRun { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
