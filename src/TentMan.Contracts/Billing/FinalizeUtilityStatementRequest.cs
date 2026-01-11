using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to finalize a utility statement.
/// </summary>
public sealed class FinalizeUtilityStatementRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
