using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Junction table linking files to units.
/// </summary>
public class UnitFile : BaseEntity
{
    public UnitFile()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        SortOrder = 0;
    }

    public Guid UnitId { get; set; }
    public Guid FileId { get; set; }
    public FileTag FileTag { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public Unit Unit { get; set; } = null!;
    public FileMetadata File { get; set; } = null!;
}
