using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Archu.Api.Contracts.Products;

/// <summary>
/// Captures the inputs required to create a product so workflow steps that
/// gather catalog information can submit a well-structured payload.
/// </summary>
public sealed class CreateProductRequest : IValidatableObject
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    /// <summary>
    /// Ensures that the workflow only forwards prices containing at most two
    /// decimal places so downstream catalog processes receive normalized data.
    /// </summary>
    /// <param name="validationContext">Validation metadata supplied by the pipeline.</param>
    /// <returns>Validation results indicating whether the price precision is acceptable.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(Price, 2, MidpointRounding.AwayFromZero) != Price)
        {
            yield return new ValidationResult(
                "Price must contain at most two decimal places.",
                new[] { nameof(Price) });
        }
    }
}
