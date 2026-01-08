using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// DTO for lease list display.
/// </summary>
public class LeaseListDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? LeaseNumber { get; set; }
    public LeaseStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? PrimaryTenantName { get; set; }
    public decimal? CurrentRent { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
