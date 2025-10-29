using System.Collections.Generic;
using Archu.Application.Products.Queries.GetProducts;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class GetProductsQueryHandlerTests
{
    #region Happy Path Tests

    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        // Normalize to valid pagination range
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);
        totalCount = Math.Max(1, Math.Abs(totalCount % 1000) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        
        var expectedItems = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        result.Items.Should().HaveCount(expectedItems);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnEmptyPagedResult_WhenNoProductsExist(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldMapPropertiesCorrectly(
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var products = new List<Product>
        {
            new()
            {
                Id = productId,
                Name = productName,
                Price = price,
                RowVersion = rowVersion
            }
        };

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>();

        fixture.MockProductRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Items.First();
        dto.Id.Should().Be(productId);
        dto.Name.Should().Be(productName);
        dto.Price.Should().Be(price);
        dto.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    #endregion

    #region Pagination Tests

    [Theory]
    [InlineData(1, 10, 5)]
    [InlineData(2, 10, 25)]
    [InlineData(1, 20, 100)]
    [InlineData(5, 10, 50)]
    [InlineData(1, 50, 200)]
    public async Task Handle_CalculatesPaginationCorrectly(int pageNumber, int pageSize, int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: pageNumber, PageSize: pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var expectedItems = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        result.Items.Should().HaveCount(expectedItems);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(10, 25)]   // Total: 25, PageSize: 10, Expected: 3 pages
    [InlineData(10, 100)]  // Total: 100, PageSize: 10, Expected: 10 pages
    [InlineData(20, 100)]  // Total: 100, PageSize: 20, Expected: 5 pages
    [InlineData(15, 44)]   // Total: 44, PageSize: 15, Expected: 3 pages
    public async Task Handle_CalculatesTotalPagesCorrectly(int pageSize, int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, currentPage: 1);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: 1, PageSize: pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var expectedPages = (int)Math.Ceiling((double)totalCount / pageSize);
        result.TotalPages.Should().Be(expectedPages);
    }

    [Theory]
    [InlineData(1, 10, 25, true, false)]   // First page, has next
    [InlineData(2, 10, 25, true, true)]    // Middle page, has both
    [InlineData(3, 10, 25, false, true)]   // Last page, has previous
    [InlineData(1, 10, 5, false, false)]   // Only page
    public async Task Handle_CalculatesHasNextAndHasPreviousCorrectly(
        int pageNumber,
        int pageSize,
        int totalCount,
        bool expectedHasNext,
        bool expectedHasPrevious)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: pageNumber, PageSize: pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasNext.Should().Be(expectedHasNext);
        result.HasPrevious.Should().Be(expectedHasPrevious);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultPaginationParameters()
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery();

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetPagedCalled(1, 10);
    }

    #endregion

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenRetrievingProducts(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 15, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "PageNumber", pageNumber },
            { "PageSize", pageSize },
            { "{OriginalFormat}", "Retrieving products - Page {PageNumber}, Size {PageSize}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductsRetrieved(
        int totalCount)
    {
        // Arrange
        totalCount = Math.Max(1, Math.Abs(totalCount % 100) + 1);
        var pageSize = 10;
        var itemsReturned = Math.Min(pageSize, totalCount);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, currentPage: 1);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: 1, PageSize: pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "Count", itemsReturned },
            { "TotalCount", totalCount },
            { "{OriginalFormat}", "Retrieved {Count} products out of {TotalCount} total" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 15, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    #endregion

    #region Structured Logging Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredPaginationParameters(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);
        totalCount = Math.Max(1, Math.Abs(totalCount % 1000) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "PageNumber", pageNumber },
            { "PageSize", pageSize }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredResultCount(
        int totalCount)
    {
        // Arrange
        totalCount = Math.Max(1, Math.Abs(totalCount % 100) + 1);
        var pageSize = 10;
        var itemsReturned = Math.Min(pageSize, totalCount);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, currentPage: 1);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(PageNumber: 1, PageSize: pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert - Match actual field names from the handler: {Count} and {TotalCount}
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "Count", itemsReturned },
            { "TotalCount", totalCount }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesPageNumberInInitialLog(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 50, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?> { { "PageNumber", pageNumber } },
            Times.Once());
        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?> { { "PageSize", pageSize } },
            Times.Once());
    }

    #endregion

    #region Cancellation Token Flow Tests

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToGetPagedAsync(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, cancellationToken);

        // Assert
        fixture.VerifyGetPagedCalledWithToken(pageNumber, pageSize, cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithCancelledOperation();

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    #endregion

    #region Repository Interaction Tests

    [Theory, AutoMoqData]
    public async Task Handle_CallsRepositoryOnlyOnce(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetPagedCalled(pageNumber, pageSize, Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldNotCallGetAll_WhenPaginating(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, Math.Abs(pageNumber % 10) + 1);
        pageSize = Math.Max(1, Math.Abs(pageSize % 100) + 1);

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 10000, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCountLessOrEqualTo(pageSize);
        fixture.VerifyGetAllCalled(Times.Never());
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData(1, 100, 10000)]   // Large dataset - first page
    [InlineData(100, 100, 10000)] // Large dataset - last page
    [InlineData(50, 100, 10000)]  // Large dataset - middle page
    public async Task Handle_HandlesLargeDatasets_CorrectlyCalculatesPagination(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var expectedItems = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        result.Items.Should().HaveCount(expectedItems);
        result.TotalCount.Should().Be(totalCount);
        result.TotalPages.Should().Be((int)Math.Ceiling((double)totalCount / pageSize));
    }

    [Theory]
    [InlineData(101, 100, 10000)]  // Beyond last page
    [InlineData(200, 100, 10000)]  // Way beyond last page
    public async Task Handle_WhenPageNumberExceedsTotalPages_ReturnsEmptyItems(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
    }

    #endregion
}
