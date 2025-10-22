using Archu.Domain.Abstractions.Identity;
using Archu.Domain.Common;

namespace Archu.Domain.Entities;

public class Product : BaseEntity, IHasOwner
{
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
