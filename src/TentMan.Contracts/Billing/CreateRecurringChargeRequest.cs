using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to create a recurring charge for a lease.
/// </summary>
public sealed class CreateRecurringChargeRequest
{
    [Required]
    public Guid ChargeTypeId { get; init; }
    
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; init; }
    
    [Required]
    public BillingFrequency Frequency { get; init; }
    
    [Required]
    public DateOnly StartDate { get; init; }
    
    public DateOnly? EndDate { get; init; }
    
    public bool IsActive { get; init; } = true;
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
}
