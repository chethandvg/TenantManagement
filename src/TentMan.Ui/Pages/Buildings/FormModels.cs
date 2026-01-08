using TentMan.Contracts.Enums;

namespace TentMan.Ui.Pages.Buildings;

/// <summary>
/// Form model for creating/editing a unit.
/// </summary>
public class UnitFormModel
{
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; } = 1;
    public UnitType UnitType { get; set; } = UnitType.OneBHK;
    public decimal AreaSqFt { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public Furnishing Furnishing { get; set; } = Furnishing.Unfurnished;
    public int ParkingSlots { get; set; }
}

/// <summary>
/// Form model for building address.
/// </summary>
public class AddressFormModel
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string Locality { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
