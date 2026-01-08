using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Junction table linking files to buildings.
/// </summary>
public class BuildingFile : BaseEntity
{
    public BuildingFile()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        SortOrder = 0;
    }

    public Guid BuildingId { get; set; }
    public Guid FileId { get; set; }
    public FileTag FileTag { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public Building Building { get; set; } = null!;
    public FileMetadata File { get; set; } = null!;
}
