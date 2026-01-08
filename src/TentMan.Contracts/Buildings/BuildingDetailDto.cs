using TentMan.Domain.Enums;

namespace TentMan.Contracts.Buildings;

public sealed class BuildingDetailDto
{
    public Guid Id { get; init; }
    public Guid OrgId { get; init; }
    public string BuildingCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PropertyType PropertyType { get; init; }
    public int TotalFloors { get; init; }
    public bool HasLift { get; init; }
    public string? Notes { get; init; }
    public BuildingAddressDto? Address { get; init; }
    public List<OwnerShareDto> CurrentOwners { get; init; } = new();
    public List<FileDto> Files { get; init; } = new();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class BuildingAddressDto
{
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string Locality { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string? Landmark { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

public sealed class OwnerShareDto
{
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public decimal SharePercent { get; init; }
}

public sealed class FileDto
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public FileTag FileTag { get; init; }
    public long SizeBytes { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
