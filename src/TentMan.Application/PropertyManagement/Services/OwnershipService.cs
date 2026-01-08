using TentMan.Contracts.Buildings;

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

    /// <summary>
    /// Validates ownership share requests for completeness and correctness.
    /// Checks for: non-empty list, no duplicate owners, positive shares, and sum equals 100%.
    /// </summary>
    /// <param name="shares">The ownership share requests to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    void ValidateOwnershipShareRequests(List<OwnershipShareRequest> shares);
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

    public void ValidateOwnershipShareRequests(List<OwnershipShareRequest> shares)
    {
        if (shares == null || shares.Count == 0)
        {
            throw new InvalidOperationException("At least one ownership share is required");
        }

        // Check for duplicate owners
        var duplicateOwners = shares.GroupBy(s => s.OwnerId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOwners.Count != 0)
        {
            throw new InvalidOperationException("Each owner can only appear once in the ownership set");
        }

        // Check all shares are positive
        var invalidShares = shares.Where(s => s.SharePercent <= 0).ToList();
        if (invalidShares.Count != 0)
        {
            throw new InvalidOperationException("All ownership shares must be greater than 0");
        }

        // Validate sum equals 100%
        var sharePercents = shares.Select(s => s.SharePercent);
        if (!ValidateOwnershipShares(sharePercents))
        {
            var error = GetOwnershipValidationError(sharePercents);
            throw new InvalidOperationException(error);
        }
    }
}
