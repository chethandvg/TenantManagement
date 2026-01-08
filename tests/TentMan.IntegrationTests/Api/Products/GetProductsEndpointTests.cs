using System.Net;
using TentMan.Contracts.Common;
using TentMan.Contracts.Products;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using TentMan.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace TentMan.IntegrationTests.Api.Products;

[Collection("Integration Tests")]
public class GetProductsEndpointTests : IAsyncLifetime
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private List<Product> _testProducts = new();
    private Guid _testUserId = Guid.NewGuid();

    public GetProductsEndpointTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        // Create a test user first to satisfy the foreign key constraint
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "testuser",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            PasswordHash = "dummy-hash-for-testing",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        dbContext.Users.Add(testUser);
        await dbContext.SaveChangesAsync();

        _testProducts = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Product 1",
                Price = 10.99m,
                OwnerId = _testUserId
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Product 2",
                Price = 20.99m,
                OwnerId = _testUserId
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Product 3",
                Price = 30.99m,
                OwnerId = _testUserId
            }
        };

        dbContext.Products.AddRange(_testProducts);
        await dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetProducts_ShouldReturn200OK_WithPagedProducts()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Items.Should().HaveCount(3);
        apiResponse.Data.PageNumber.Should().Be(1);
        apiResponse.Data.PageSize.Should().Be(10);
        apiResponse.Data.TotalCount.Should().Be(3);
        apiResponse.Data.TotalPages.Should().Be(1);
        apiResponse.Message.Should().Be("Products retrieved successfully");
    }

    [Fact]
    public async Task GetProducts_ShouldReturnCorrectProductData()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        var products = apiResponse!.Data!.Items.ToList();
        products.Should().Contain(p => p.Name == "Integration Test Product 1" && p.Price == 10.99m);
        products.Should().Contain(p => p.Name == "Integration Test Product 2" && p.Price == 20.99m);
        products.Should().Contain(p => p.Name == "Integration Test Product 3" && p.Price == 30.99m);

        products.Should().AllSatisfy(p =>
        {
            p.Id.Should().NotBeEmpty();
            p.RowVersion.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task GetProducts_ShouldReturn401_WhenUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnEmptyPagedResult_WhenNoProducts()
    {
        await _factory.ResetDatabaseAsync();

        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Items.Should().BeEmpty();
        apiResponse.Data.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    public async Task GetProducts_ShouldAllowAccessForAuthenticatedRoles(string role)
    {
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_ShouldHaveCorrectContentType()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetProducts_ShouldNotIncludeSoftDeletedProducts()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var deletedProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Product",
            Price = 99.99m,
            OwnerId = _testUserId,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        };

        dbContext.Products.Add(deletedProduct);
        await dbContext.SaveChangesAsync();

        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        apiResponse!.Data!.Items.Should().NotContain(p => p.Name == "Deleted Product");
        apiResponse.Data.Items.Should().HaveCount(3);
        apiResponse.Data.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetProducts_ShouldHandleConcurrentRequests()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync("/api/v1/products"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task GetProducts_ShouldIncludeTimestamp()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var beforeRequest = DateTime.UtcNow;

        var response = await _client.GetAsync("/api/v1/products");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        var afterRequest = DateTime.UtcNow;

        apiResponse!.Timestamp.Should().BeOnOrAfter(beforeRequest.AddSeconds(-1));
        apiResponse.Timestamp.Should().BeOnOrBefore(afterRequest.AddSeconds(1));
    }

    [Theory]
    [InlineData(1, 2, 3, 2)]
    [InlineData(2, 2, 3, 2)]
    [InlineData(1, 10, 3, 1)]
    [InlineData(1, 1, 3, 3)]
    public async Task GetProducts_ShouldHandleDifferentPaginationParameters(int pageNumber, int pageSize, int expectedTotalCount, int expectedTotalPages)
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/products?pageNumber={pageNumber}&pageSize={pageSize}");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        apiResponse!.Data!.PageNumber.Should().Be(pageNumber);
        apiResponse.Data.PageSize.Should().Be(pageSize);
        apiResponse.Data.TotalCount.Should().Be(expectedTotalCount);
        apiResponse.Data.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public async Task GetProducts_ShouldEnforceMaxPageSize()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products?pageNumber=1&pageSize=200");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        apiResponse!.Data!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetProducts_ShouldUseDefaultPaginationWhenNotSpecified()
    {
        var token = await _factory.GetJwtTokenAsync("User", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/products");
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();

        apiResponse!.Data!.PageNumber.Should().Be(1);
        apiResponse.Data.PageSize.Should().Be(10);
    }
}
