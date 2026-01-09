using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a document associated with a tenant (e.g., ID proof, address proof).
/// </summary>
public class TenantDocument : BaseEntity
{
    public TenantDocument()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid TenantId { get; set; }
    public Guid? LeaseId { get; set; } // Some docs are lease-specific
    public DocumentType DocType { get; set; }
    public string? DocNumberMasked { get; set; } // Never store full Aadhaar/PAN raw; mask
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public Guid FileId { get; set; }
    public string? Notes { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending; // Default to Pending
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Lease? Lease { get; set; }
    public FileMetadata File { get; set; } = null!;
}
