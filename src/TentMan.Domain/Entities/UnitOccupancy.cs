using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents the occupancy history of a unit.
/// Useful for reporting and tracking vacancy periods.
/// </summary>
public class UnitOccupancy : BaseEntity
{
    public UnitOccupancy()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid UnitId { get; set; }
    public Guid LeaseId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public UnitOccupancyHistoryStatus Status { get; set; }

    // Navigation properties
    public Unit Unit { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
}
