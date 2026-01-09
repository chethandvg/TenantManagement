using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an item in a handover checklist (e.g., furniture condition).
/// </summary>
public class HandoverChecklistItem : BaseEntity
{
    public HandoverChecklistItem()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid HandoverId { get; set; }
    public string Category { get; set; } = string.Empty; // Electrical, Plumbing, Furniture...
    public string ItemName { get; set; } = string.Empty;
    public ItemCondition Condition { get; set; }
    public string? Remarks { get; set; }
    public Guid? PhotoFileId { get; set; }

    // Navigation properties
    public UnitHandover Handover { get; set; } = null!;
    public FileMetadata? PhotoFile { get; set; }
}
