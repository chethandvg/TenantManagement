using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to update a recurring charge.
/// </summary>
public sealed class UpdateRecurringChargeRequest
{
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
    
    public bool IsActive { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
