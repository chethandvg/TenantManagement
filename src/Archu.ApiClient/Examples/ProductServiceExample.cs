using Archu.ApiClient.Exceptions;
using Archu.ApiClient.Helpers;
using Archu.ApiClient.Services;
using Archu.Contracts.Products;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Examples;

/// <summary>
/// Example usage of the API client with exception handling.
/// </summary>
public class ProductServiceExample
{
    private readonly IProductsApiClient _productsApiClient;
    private readonly ILogger<ProductServiceExample> _logger;

    public ProductServiceExample(
        IProductsApiClient productsApiClient,
        ILogger<ProductServiceExample> logger)
    {
        _productsApiClient = productsApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Example: Get product with comprehensive exception handling.
    /// </summary>
    public async Task<ProductDto?> GetProductWithExceptionHandlingAsync(Guid productId)
    {
        try
        {
            var response = await _productsApiClient.GetProductByIdAsync(productId);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Failed to get product {ProductId}: {Message}",
                    productId,
                    response.Message);
                return null;
            }

            return response.Data;
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Product {ProductId} not found",
                productId);
            return null;
        }
        catch (AuthorizationException ex)
        {
            ExceptionHandler.HandleException(ex, _logger, "GetProduct");
            throw; // Re-throw to let caller handle authentication
        }
        catch (ApiClientException ex)
        {
            ExceptionHandler.HandleException(ex, _logger, "GetProduct");
            throw; // Re-throw for critical errors
        }
    }

    /// <summary>
    /// Example: Create product with validation error handling.
    /// </summary>
    public async Task<(bool Success, ProductDto? Product, string ErrorMessage)> CreateProductAsync(
        CreateProductRequest request)
    {
        try
        {
            var response = await _productsApiClient.CreateProductAsync(request);

            if (response.Success && response.Data != null)
            {
                return (true, response.Data, string.Empty);
            }

            var errorMessage = response.Message ?? "Failed to create product";
            return (false, null, errorMessage);
        }
        catch (ValidationException ex)
        {
            var userMessage = ExceptionHandler.GetUserFriendlyMessage(ex);
            _logger.LogWarning(
                ex,
                "Validation failed while creating product: {Errors}",
                string.Join(", ", ex.Errors));
            
            return (false, null, userMessage);
        }
        catch (Exception ex)
        {
            ExceptionHandler.HandleException(ex, _logger, "CreateProduct");
            var userMessage = ExceptionHandler.GetUserFriendlyMessage(ex);
            return (false, null, userMessage);
        }
    }

    /// <summary>
    /// Example: Update product with retry logic for retryable errors.
    /// </summary>
    public async Task<ProductDto?> UpdateProductWithRetryAsync(
        Guid productId,
        UpdateProductRequest request,
        int maxRetries = 3)
    {
        int retryCount = 0;

        while (retryCount <= maxRetries)
        {
            try
            {
                var response = await _productsApiClient.UpdateProductAsync(productId, request);

                if (response.Success)
                {
                    return response.Data;
                }

                _logger.LogWarning(
                    "Failed to update product {ProductId}: {Message}",
                    productId,
                    response.Message);
                return null;
            }
            catch (Exception ex) when (ExceptionHandler.IsRetryable(ex) && retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                
                _logger.LogWarning(
                    ex,
                    "Retryable error updating product {ProductId}. Retry {RetryCount}/{MaxRetries} after {Delay}s",
                    productId,
                    retryCount,
                    maxRetries,
                    delay.TotalSeconds);

                await Task.Delay(delay);
            }
            catch (ResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product {ProductId} not found", productId);
                return null;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Validation failed updating product {ProductId}: {Errors}",
                    productId,
                    string.Join(", ", ex.Errors));
                throw; // Don't retry validation errors
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, _logger, "UpdateProduct");
                throw;
            }
        }

        _logger.LogError(
            "Failed to update product {ProductId} after {MaxRetries} retries",
            productId,
            maxRetries);
        return null;
    }

    /// <summary>
    /// Example: Delete product with user-friendly error messages.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteProductAsync(Guid productId)
    {
        try
        {
            var response = await _productsApiClient.DeleteProductAsync(productId);

            if (response.Success)
            {
                return (true, "Product deleted successfully");
            }

            return (false, response.Message ?? "Failed to delete product");
        }
        catch (ResourceNotFoundException)
        {
            return (false, "Product not found");
        }
        catch (AuthorizationException ex) when (ex.StatusCode == 401)
        {
            return (false, "Please log in to delete products");
        }
        catch (AuthorizationException)
        {
            return (false, "You don't have permission to delete this product");
        }
        catch (Exception ex)
        {
            ExceptionHandler.HandleException(ex, _logger, "DeleteProduct");
            var userMessage = ExceptionHandler.GetUserFriendlyMessage(ex);
            return (false, userMessage);
        }
    }

    /// <summary>
    /// Example: Batch operation with individual error handling.
    /// </summary>
    public async Task<(int SuccessCount, int FailureCount, List<string> Errors)> GetMultipleProductsAsync(
        IEnumerable<Guid> productIds)
    {
        var successCount = 0;
        var failureCount = 0;
        var errors = new List<string>();

        foreach (var productId in productIds)
        {
            try
            {
                var response = await _productsApiClient.GetProductByIdAsync(productId);

                if (response.Success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                    errors.Add($"Failed to get product {productId}: {response.Message}");
                }
            }
            catch (ResourceNotFoundException)
            {
                failureCount++;
                errors.Add($"Product {productId} not found");
            }
            catch (Exception ex)
            {
                failureCount++;
                var message = ExceptionHandler.GetUserFriendlyMessage(ex);
                errors.Add($"Error getting product {productId}: {message}");
                
                _logger.LogWarning(
                    ex,
                    "Error getting product {ProductId}",
                    productId);
            }
        }

        return (successCount, failureCount, errors);
    }
}
