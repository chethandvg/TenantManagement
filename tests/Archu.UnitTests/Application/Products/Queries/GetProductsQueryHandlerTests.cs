using Archu.Application.Products.Queries.GetProducts;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class GetProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 5, pageSize: 10, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPagedResult_WhenNoProductsExist()
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
    }

    [Fact]
    public async Task Handle_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var products = new List<Product>
        {
            new()
            {
                Id = productId,
                Name = "Test Product",
                Price = 99.99m,
                RowVersion = rowVersion
            }
        };

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>();

        fixture.MockProductRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Items.First();
        dto.Id.Should().Be(productId);
        dto.Name.Should().Be("Test Product");
        dto.Price.Should().Be(99.99m);
        dto.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
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
            async () => await handler.Handle(query, cts.Token));
    }

    [Fact]
    public async Task Handle_ShouldLogInformationMessages()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 15, pageSize: 10, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged("Retrieving products");
        fixture.VerifyInformationLogged("Retrieved 10 products");
    }

    [Theory]
    [InlineData(1, 10, 5)]
    [InlineData(2, 10, 25)]
    [InlineData(1, 20, 100)]
    [InlineData(5, 10, 50)]
    public async Task Handle_ShouldHandleDifferentPaginationParameters(int pageNumber, int pageSize, int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: pageNumber, PageSize: pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var itemsToReturn = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        result.Items.Should().HaveCount(itemsToReturn);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 25, pageSize: 10, currentPage: 1);

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetPagedCalled(1, 10);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultPaginationParameters()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = new GetProductsQueryHandler(
            fixture.MockProductRepository.Object,
            fixture.MockLogger.Object);

        var query = new GetProductsQuery();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetPagedCalled(1, 10);
    }
}
