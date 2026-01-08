using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Products;

/// <summary>
/// Captures the edits that operators submit when refreshing product details.
/// Includes RowVersion for optimistic concurrency control to prevent lost updates.
/// </summary>
public sealed class UpdateProductRequest : IValidatableObject
{
    [Required]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; init; }

    /// <summary>
    /// The RowVersion from the previous GET operation, used for optimistic concurrency control.
    /// This ensures the client is updating the same version of the product they retrieved.
    /// </summary>
    [Required]
    [MinLength(1)]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Confirms that price updates stay within the two-decimal precision agreed
    /// upon by the product management workflow.
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
