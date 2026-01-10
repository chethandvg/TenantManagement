using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a batch billing run that generates invoices for multiple leases.
/// Used for monthly billing cycles across all active leases.
/// </summary>
public class InvoiceRun : BaseEntity
{
    public InvoiceRun()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Status = InvoiceRunStatus.Pending;
    }

    public Guid OrgId { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public DateOnly BillingPeriodStart { get; set; }
    public DateOnly BillingPeriodEnd { get; set; }
    public InvoiceRunStatus Status { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int TotalLeases { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<InvoiceRunItem> Items { get; set; } = new List<InvoiceRunItem>();
}
