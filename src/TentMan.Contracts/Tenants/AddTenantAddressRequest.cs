using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// Request to add an address for a tenant.
/// </summary>
public class AddTenantAddressRequest
{
    public AddressType Type { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "IN";
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool IsPrimary { get; set; }
}
