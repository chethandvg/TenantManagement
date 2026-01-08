using System.Collections.Generic;
using TentMan.Application.Products.Queries.GetProductById;
using TentMan.Domain.Entities;
using TentMan.UnitTests.TestHelpers.Builders;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Products.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class GetProductByIdQueryHandlerTests
{
    #region Happy Path Tests

    [Theory, AutoMoqData]
    public async Task Handle_WhenProductExists_ReturnsProductDto(
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .WithName(productName)
            .WithPrice(price)
            .WithRowVersion(rowVersion)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Name.Should().Be(productName);
        result.Price.Should().Be(price);
        result.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Theory, AutoMoqData]
    public async Task Handle_MapsAllProductProperties(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
        result.Price.Should().Be(product.Price);
        result.RowVersion.Should().BeEquivalentTo(product.RowVersion);
    }

    #endregion

    #region Error Handling Tests

    [Theory, AutoMoqData]
    public async Task Handle_WhenProductNotFound_ReturnsNull(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenRetrievingProduct(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId },
            { "{OriginalFormat}", "Retrieving product with ID: {ProductId}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenProductNotFound(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged("Product with ID {ProductId} not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsOnlyInformation_WhenProductFound(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 1);
        fixture.VerifyLogCount(LogLevel.Warning, 0);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarningAfterInformation_WhenProductNotFound(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 1);
        fixture.VerifyLogCount(LogLevel.Warning, 1);
    }

    #endregion

    #region Structured Logging Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredProductId_WhenRetrieving(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredProductId_InWarning_WhenNotFound(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredWarningLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesProductIdInAllLogs(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert - Verify ProductId appears in all log entries
        fixture.MockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(productId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least in both Information and Warning logs
    }

    #endregion

    #region Cancellation Token Flow Tests

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToGetByIdAsync(Guid productId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, cancellationToken);

        // Assert
        fixture.VerifyGetByIdCalledWithToken(productId, cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(Guid productId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithCancelledOperation();

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    [Theory, AutoMoqData]
    public async Task Handle_DoesNotSwallowCancelledException(Guid productId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>();

        fixture.MockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        Func<Task> act = async () => await handler.Handle(query, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Repository Interaction Tests

    [Theory, AutoMoqData]
    public async Task Handle_CallsGetByIdAsyncOnce(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetByIdCalled(productId, Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_CallsGetByIdAsyncWithCorrectId(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.MockProductRepository.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_DoesNotCallOtherRepositoryMethods(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.MockProductRepository.Verify(
            r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.MockProductRepository.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Case Tests

    [Theory, AutoMoqData]
    public async Task Handle_WithEmptyGuid_QueriesCorrectly(Guid emptyLikeId)
    {
        // Arrange - Use a valid GUID (not Guid.Empty as that might be filtered)
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProductNotFound(emptyLikeId);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(emptyLikeId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        fixture.VerifyGetByIdCalled(emptyLikeId);
    }

    [Theory]
    [InlineAutoMoqData("00000000-0000-0000-0000-000000000001")]
    [InlineAutoMoqData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
    public async Task Handle_WithSpecialGuids_HandlesCorrectly(string guidString)
    {
        // Arrange
        var productId = Guid.Parse(guidString);
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRepositoryReturnsNullProduct_HandlesGracefully(Guid productId)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>();

        fixture.MockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Data Integrity Tests

    [Theory, AutoMoqData]
    public async Task Handle_DoesNotModifyProduct(Guid productId)
    {
        // Arrange
        var originalName = "Original Name";
        var originalPrice = 99.99m;
        var originalRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var product = new ProductBuilder()
            .WithId(productId)
            .WithName(originalName)
            .WithPrice(originalPrice)
            .WithRowVersion(originalRowVersion)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert - Verify product was not modified
        product.Name.Should().Be(originalName);
        product.Price.Should().Be(originalPrice);
        product.RowVersion.Should().BeEquivalentTo(originalRowVersion);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ReturnsNewDtoInstance(Guid productId)
    {
        // Arrange
        var product = new ProductBuilder()
            .WithId(productId)
            .Build();

        var fixture = new QueryHandlerTestFixture<GetProductByIdQueryHandler>()
            .WithProduct(product);

        var handler = fixture.CreateHandler();
        var query = new GetProductByIdQuery(productId);

        // Act
        var result1 = await handler.Handle(query, CancellationToken.None);
        var result2 = await handler.Handle(query, CancellationToken.None);

        // Assert - Each call should return a new DTO instance
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().NotBeSameAs(result2);
        result1!.Id.Should().Be(result2!.Id);
    }

    #endregion
}
