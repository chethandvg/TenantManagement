using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Audit trail for payment status changes.
/// Tracks who changed the status, when, and why.
/// </summary>
public class PaymentStatusHistory : BaseEntity
{
    public PaymentStatusHistory()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid PaymentId { get; set; }
    
    /// <summary>
    /// Previous status before this change
    /// </summary>
    public PaymentStatus FromStatus { get; set; }
    
    /// <summary>
    /// New status after this change
    /// </summary>
    public PaymentStatus ToStatus { get; set; }
    
    /// <summary>
    /// Timestamp when the status changed
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }
    
    /// <summary>
    /// User ID or system identifier who made the change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason or notes for the status change
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Additional metadata (e.g., gateway response, error details) stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation property
    public Payment Payment { get; set; } = null!;
}
