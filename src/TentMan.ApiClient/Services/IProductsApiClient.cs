using TentMan.Contracts.Common;
using TentMan.Contracts.Products;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Products API client.
/// </summary>
public interface IProductsApiClient
{
    /// <summary>
    /// Gets a paginated list of products.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of products.</returns>
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The product details.</returns>
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created product.</returns>
    Task<ApiResponse<ProductDto>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="request">The product update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated product.</returns>
    Task<ApiResponse<ProductDto>> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating success or failure.</returns>
    Task<ApiResponse<bool>> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
