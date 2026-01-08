using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a move-in or move-out handover record for a unit.
/// </summary>
public class UnitHandover : BaseEntity
{
    public UnitHandover()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        SignedByTenant = false;
        SignedByOwner = false;
    }

    public Guid LeaseId { get; set; }
    public HandoverType Type { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public bool SignedByTenant { get; set; }
    public bool SignedByOwner { get; set; }
    public Guid? SignatureTenantFileId { get; set; }
    public Guid? SignatureOwnerFileId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Lease Lease { get; set; } = null!;
    public FileMetadata? SignatureTenantFile { get; set; }
    public FileMetadata? SignatureOwnerFile { get; set; }
    public ICollection<HandoverChecklistItem> ChecklistItems { get; set; } = new List<HandoverChecklistItem>();
}
