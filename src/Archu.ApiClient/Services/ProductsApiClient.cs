using Archu.Contracts.Common;
using Archu.Contracts.Products;

namespace Archu.ApiClient.Services;

/// <summary>
/// Implementation of the Products API client.
/// </summary>
public sealed class ProductsApiClient : ApiClientServiceBase, IProductsApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    public ProductsApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/products";

    /// <inheritdoc/>
    public Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<PagedResult<ProductDto>>(
            $"?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<ProductDto>> GetProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<ProductDto>($"{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<ProductDto>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateProductRequest, ProductDto>(string.Empty, request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<ProductDto>> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateProductRequest, ProductDto>($"{id}", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<bool>> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{id}", cancellationToken);
    }
}
