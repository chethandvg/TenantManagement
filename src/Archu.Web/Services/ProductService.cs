using System.Net.Http.Json;

namespace Archu.Web.Services;

/// <summary>
/// HTTP-based implementation of product service
/// </summary>
public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "api/products";

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        try
        {
            var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>(BaseUrl);
            return products ?? new List<ProductDto>();
        }
        catch (HttpRequestException ex)
        {
            // Log error
            throw new ApplicationException("Failed to fetch products", ex);
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"{BaseUrl}/{id}");
        }
        catch (HttpRequestException ex)
        {
            // Log error
            throw new ApplicationException($"Failed to fetch product with ID {id}", ex);
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl, request);
            response.EnsureSuccessStatusCode();
            
            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            return product ?? throw new InvalidOperationException("Failed to deserialize created product");
        }
        catch (HttpRequestException ex)
        {
            throw new ApplicationException("Failed to create product", ex);
        }
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", request);
            response.EnsureSuccessStatusCode();
            
            var product = await response.Content.ReadFromJsonAsync<ProductDto>();
            return product ?? throw new InvalidOperationException("Failed to deserialize updated product");
        }
        catch (HttpRequestException ex)
        {
            throw new ApplicationException($"Failed to update product with ID {id}", ex);
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            throw new ApplicationException($"Failed to delete product with ID {id}", ex);
        }
    }
}
