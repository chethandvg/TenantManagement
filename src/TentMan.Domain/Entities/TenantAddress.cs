using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an address associated with a tenant.
/// </summary>
public class TenantAddress : BaseEntity
{
    public TenantAddress()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Country = "IN";
    }

    public Guid TenantId { get; set; }
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

    // Navigation property
    public Tenant Tenant { get; set; } = null!;
}
