using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Files;

/// <summary>
/// DTO for file metadata.
/// </summary>
public class FileMetadataDto
{
    public Guid Id { get; set; }
    public StorageProvider StorageProvider { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
