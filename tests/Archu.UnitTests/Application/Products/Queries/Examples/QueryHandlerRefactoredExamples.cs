using Archu.Application.Products.Queries.GetProductById;
using Archu.Application.Products.Queries.GetProducts;
using Archu.UnitTests.TestHelpers.Builders;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Queries.Examples;

/// <summary>
/// Example tests demonstrating the use of QueryHandlerTestFixture for query operations.
/// This file shows the recommended pattern for writing query handler tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Example", "QueryFixture")]
public class QueryHandlerRefactoredExamples
{
    #region GetProducts Query Examples

    [Fact]
    public async Task GetProducts_WithPagedResults_ReturnsCorrectPage()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize: 10, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(100);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(10);

        fixture.VerifyGetPagedCalled(1, 10);
    }

    [Fact]
    public async Task GetProducts_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);

        fixture.VerifyGetPagedCalled();
    }

    [Fact]
    public async Task GetProducts_LogsRetrievalOperation()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 50, pageSize: 10, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged("Retrieving products");
    }

    [Fact]
    public async Task GetProducts_WithLargePageNumber_ReturnsEmptyResults()
    {
        // Arrange - Total 25 products, page size 10, requesting page 5 (beyond last page)
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 25, pageSize: 10, currentPage: 5);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 5, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(25);
    }

    #endregion

    #region GetProductById Query Examples

    [Fact]
    public async Task GetProductById_WhenProductExists_ReturnsProduct()
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName("Test Product")
            .WithPrice(99.99m)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(existingProduct);

        var handler = new GetProductByIdQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductByIdQuery(existingProduct.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingProduct.Id);
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);

        fixture.VerifyGetByIdCalled(existingProduct.Id);
    }

    [Fact]
    public async Task GetProductById_WhenProductNotFound_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = new GetProductByIdQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        fixture.VerifyGetByIdCalled(productId);
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }

    [Fact]
    public async Task GetProductById_LogsRetrievalOperation()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(existingProduct);

        var handler = new GetProductByIdQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Retrieving product with ID: {productId}");
    }

    [Fact]
    public async Task GetProductById_WithMultipleProducts_ReturnsCorrectOne()
    {
        // Arrange
        var product1 = new ProductBuilder().WithName("Product 1").Build();
        var product2 = new ProductBuilder().WithName("Product 2").Build();
        var product3 = new ProductBuilder().WithName("Product 3").Build();

        // Setup multiple products
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product1)
            .WithProduct(product2)
            .WithProduct(product3);

        var handler = new GetProductByIdQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductByIdQuery(product2.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product2.Id);
        result.Name.Should().Be("Product 2");

        fixture.VerifyGetByIdCalled(product2.Id);
    }

    #endregion

    #region Performance and Edge Cases

    [Fact]
    public async Task GetProducts_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>();

        fixture.MockProductRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    [Fact]
    public async Task GetProducts_WithBoundaryPageSizes_HandlesCorrectly()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize: 1, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(100);
        result.TotalPages.Should().Be(100);
    }

    #endregion
}
