using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a deposit transaction for tracking deposit movements during a lease.
/// </summary>
public class DepositTransaction : BaseEntity
{
    public DepositTransaction()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid LeaseId { get; set; }
    public DepositTransactionType TxnType { get; set; }
    public decimal Amount { get; set; } // Store positive; interpret by type
    public DateOnly TxnDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation property
    public Lease Lease { get; set; } = null!;
}
