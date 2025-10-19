using Archu.Domain.Entities;

namespace Archu.Application.Abstractions;

/// <summary>
/// Defines contract for product data access operations.
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
    /// Adds a new product to the catalog.
    /// </summary>
    /// <param name="product">The product to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The added product with generated values.</returns>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product in the catalog.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a product from the catalog.
    /// </summary>
    /// <param name="id">The product identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product was found and deleted; otherwise false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product exists.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product exists; otherwise false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
