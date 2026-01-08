namespace TentMan.Application.PropertyManagement.Services;

/// <summary>
/// Service for validating and resolving ownership shares.
/// </summary>
public interface IOwnershipService
{
    /// <summary>
    /// Validates that ownership shares sum to 100%.
    /// </summary>
    /// <param name="shares">List of share percentages.</param>
    /// <param name="tolerance">Tolerance for floating point comparison (default 0.01).</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidateOwnershipShares(IEnumerable<decimal> shares, decimal tolerance = 0.01m);

    /// <summary>
    /// Gets error message for invalid ownership shares.
    /// </summary>
    string GetOwnershipValidationError(IEnumerable<decimal> shares);
}

public class OwnershipService : IOwnershipService
{
    private const decimal REQUIRED_TOTAL = 100.00m;
    private const decimal DEFAULT_TOLERANCE = 0.01m;

    public bool ValidateOwnershipShares(IEnumerable<decimal> shares, decimal tolerance = DEFAULT_TOLERANCE)
    {
        var total = shares.Sum();
        var diff = Math.Abs(total - REQUIRED_TOTAL);
        return diff <= tolerance;
    }

    public string GetOwnershipValidationError(IEnumerable<decimal> shares)
    {
        var total = shares.Sum();
        return $"Ownership shares must sum to 100%. Current sum: {total}%";
    }
}
