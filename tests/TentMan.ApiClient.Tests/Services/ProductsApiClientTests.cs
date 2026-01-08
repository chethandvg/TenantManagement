using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TentMan.ApiClient.Exceptions;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Common;
using TentMan.Contracts.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.ApiClient.Tests.Services;

public class ProductsApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ProductsApiClient>> _mockLogger;
    private readonly ProductsApiClient _apiClient;

    public ProductsApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");
        
        _mockLogger = new Mock<ILogger<ProductsApiClient>>();
        _apiClient = new ProductsApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnSuccess_WhenApiReturnsProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m, RowVersion = new byte[] { 1 } },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 20m, RowVersion = new byte[] { 2 } }
        };

        var pagedResult = PagedResult<ProductDto>.Create(products, 1, 10, 2);
        var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult);

        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetProductsAsync(pageNumber: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldUseDefaultPagination_WhenNoParametersProvided()
    {
        // Arrange
        var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(
            PagedResult<ProductDto>.Create(new List<ProductDto>(), 1, 10, 0));

        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetProductsAsync();

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(5, 50)]
    public async Task GetProductsAsync_ShouldSendCorrectQueryParameters(int pageNumber, int pageSize)
    {
        // Arrange
        var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(
            PagedResult<ProductDto>.Create(new List<ProductDto>(), pageNumber, pageSize, 0));

        _mockHttp
            .When($"https://api.test.com/api/v1/products?pageNumber={pageNumber}&pageSize={pageSize}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetProductsAsync(pageNumber, pageSize);

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnFailedResponse_When404()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"success\":false,\"message\":\"Not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.GetProductsAsync());
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnFailedResponse_When401()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"success\":false,\"message\":\"Unauthorized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetProductsAsync());
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnFailedResponse_When500()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"success\":false,\"message\":\"Server error\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ServerException>(
            async () => await _apiClient.GetProductsAsync());
    }

    [Fact]
    public async Task GetProductsAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond(async () =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.GetProductsAsync(cancellationToken: cts.Token);

        // Assert - Should return failed response for cancelled request
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task GetProductsAsync_ShouldHandleEmptyResponse()
    {
        // Arrange
        var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(
            PagedResult<ProductDto>.Create(new List<ProductDto>(), 1, 10, 0));

        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetProductsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsAsync_ShouldDeserializePagedResultCorrectly()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m, RowVersion = new byte[] { 1 } }
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 25
        };

        var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult);

        _mockHttp
            .When("https://api.test.com/api/v1/products?pageNumber=2&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetProductsAsync(pageNumber: 2, pageSize: 10);

        // Assert
        result.Data!.PageNumber.Should().Be(2);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalCount.Should().Be(25);
        result.Data.TotalPages.Should().Be(3);
        result.Data.HasPrevious.Should().BeTrue();
        result.Data.HasNext.Should().BeTrue();
    }
}
