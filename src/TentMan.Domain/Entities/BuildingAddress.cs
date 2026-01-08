using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents the address of a building (1:1 relationship).
/// </summary>
public class BuildingAddress : BaseEntity
{
    public BuildingAddress()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid BuildingId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string Locality { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty; // India PIN code
    public string? Landmark { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Navigation property
    public Building Building { get; set; } = null!;
}
