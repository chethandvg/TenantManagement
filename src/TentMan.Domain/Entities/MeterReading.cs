using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a meter reading for a unit (e.g., electricity, water, gas).
/// Used for tracking initial readings during move-in/out for billing.
/// </summary>
public class MeterReading : BaseEntity
{
    public MeterReading()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid UnitId { get; set; }
    public Guid? LeaseId { get; set; } // Link to lease if taken during move-in/out
    public MeterType MeterType { get; set; }
    public DateOnly ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
    public Guid? PhotoFileId { get; set; }
    public Guid? CapturedByUserId { get; set; }

    // Navigation properties
    public Unit Unit { get; set; } = null!;
    public Lease? Lease { get; set; }
    public FileMetadata? PhotoFile { get; set; }
}
