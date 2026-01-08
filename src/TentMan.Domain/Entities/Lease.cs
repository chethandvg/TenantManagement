using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a lease contract for a unit.
/// Only one active lease per unit is allowed.
/// </summary>
public class Lease : BaseEntity
{
    public Lease()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Status = LeaseStatus.Draft;
        GraceDays = 0;
        LateFeeType = LateFeeType.None;
        IsAutoRenew = false;
    }

    public Guid OrgId { get; set; }
    public Guid UnitId { get; set; }
    public string? LeaseNumber { get; set; } // Human-friendly
    public LeaseStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; } // Null = open-ended
    public byte RentDueDay { get; set; } // 1-28 recommended
    public byte GraceDays { get; set; }
    public short? NoticePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; }
    public decimal? LateFeeValue { get; set; }
    public string? PaymentMethodNote { get; set; } // UPI ID, bank info hint
    public string? TermsText { get; set; } // For prototype; later templates
    public bool IsAutoRenew { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public ICollection<LeaseParty> Parties { get; set; } = new List<LeaseParty>();
    public ICollection<LeaseTerm> Terms { get; set; } = new List<LeaseTerm>();
    public ICollection<DepositTransaction> DepositTransactions { get; set; } = new List<DepositTransaction>();
    public ICollection<UnitHandover> Handovers { get; set; } = new List<UnitHandover>();
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    public ICollection<TenantDocument> Documents { get; set; } = new List<TenantDocument>();
    public ICollection<UnitOccupancy> Occupancies { get; set; } = new List<UnitOccupancy>();
}
