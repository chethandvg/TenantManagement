using Archu.Domain.Abstractions.Identity;
using Archu.Domain.Common;

namespace Archu.Domain.Entities;

public class Product : BaseEntity, IHasOwner
{
    /// <summary>
    /// Initializes a new instance of the Product entity with default values.
    /// This constructor ensures the entity is in a valid state upon creation.
    /// </summary>
    public Product()
    {
        // Initialize audit fields to ensure domain invariants
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System"; // Default creator when not specified

        // Initialize owner to a non-empty Guid to satisfy IHasOwner contract
        // This should be overridden by the application when creating products
        OwnerId = Guid.NewGuid();
    }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns this product.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Checks if the specified user is the owner of this product.
    /// </summary>
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
