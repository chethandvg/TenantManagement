using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a type of charge that can be applied to billing.
/// System-seeded with standard charges (RENT, MAINT, ELEC, etc.)
/// Can also be extended with custom charge types per organization.
/// </summary>
public class ChargeType : BaseEntity
{
    public ChargeType()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsActive = true;
        IsSystemDefined = false;
    }

    public Guid? OrgId { get; set; } // Null = system-wide, not null = org-specific
    public ChargeTypeCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemDefined { get; set; } // True for seeded system records
    public bool IsTaxable { get; set; }
    public decimal? DefaultAmount { get; set; }

    // Navigation properties
    public Organization? Organization { get; set; }
    public ICollection<LeaseRecurringCharge> LeaseRecurringCharges { get; set; } = new List<LeaseRecurringCharge>();
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
