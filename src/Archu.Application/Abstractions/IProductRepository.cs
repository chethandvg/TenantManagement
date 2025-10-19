using Archu.Domain.Entities;

namespace Archu.Application.Abstractions;

/// <summary>
/// Defines contract for product data access operations.
/// Note: This repository only tracks changes. Call IUnitOfWork.SaveChangesAsync() to persist.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves all active products from the catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of products.</returns>
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product if found; otherwise null.</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a new product for addition to the catalog.
    /// Call IUnitOfWork.SaveChangesAsync() to persist.
    /// </summary>
    /// <param name="product">The product to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tracked product.</returns>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an existing product for update.
    /// Call IUnitOfWork.SaveChangesAsync() to persist.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a product for deletion (soft delete).
    /// Call IUnitOfWork.SaveChangesAsync() to persist.
    /// </summary>
    /// <param name="product">The product to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product exists.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
