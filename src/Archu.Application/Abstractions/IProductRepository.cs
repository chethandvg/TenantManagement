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
    /// Retrieves all products owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The ID of the user who owns the products.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of products owned by the user.</returns>
    Task<IEnumerable<Product>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product if found; otherwise null.</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a product by ID, ensuring it belongs to the specified owner.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="ownerId">The ID of the user who should own the product.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product if found and owned by the user; otherwise null.</returns>
    Task<Product?> GetByIdAndOwnerAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a new product for addition to the catalog.
    /// Call IUnitOfWork.SaveChangesAsync() to persist.
    /// </summary>
    /// <param name="product">The product to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tracked product.</returns>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks an existing product for update with concurrency control.
    /// Call IUnitOfWork.SaveChangesAsync() to persist.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <param name="originalRowVersion">The original RowVersion from the client for concurrency detection.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Checks if a product exists and is owned by the specified user.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="ownerId">The ID of the user who should own the product.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product exists and is owned by the user; otherwise false.</returns>
    Task<bool> ExistsAndIsOwnedByAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product is owned by the specified user without loading the entire entity.
    /// This is optimized for authorization checks where only ownership verification is needed.
    /// </summary>
    /// <param name="resourceId">The product identifier.</param>
    /// <param name="userId">The ID of the user to check ownership for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product is owned by the user; otherwise false.</returns>
    Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default);
}
