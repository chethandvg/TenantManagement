namespace TentMan.Contracts.Buildings;

public sealed class SetBuildingAddressRequest
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
