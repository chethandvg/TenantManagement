using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to update a utility statement.
/// </summary>
public sealed class UpdateUtilityStatementRequest
{
    public Guid? UtilityRatePlanId { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? PreviousReading { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? CurrentReading { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? DirectBillAmount { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
