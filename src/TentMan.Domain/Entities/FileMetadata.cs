using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents file metadata (actual files stored in storage provider).
/// </summary>
public class FileMetadata : BaseEntity
{
    public FileMetadata()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid OrgId { get; set; }
    public StorageProvider StorageProvider { get; set; }
    public string StorageKey { get; set; } = string.Empty; // Path/key in storage
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<BuildingFile> BuildingFiles { get; set; } = new List<BuildingFile>();
    public ICollection<UnitFile> UnitFiles { get; set; } = new List<UnitFile>();
    public ICollection<TenantDocument> TenantDocuments { get; set; } = new List<TenantDocument>();
}
