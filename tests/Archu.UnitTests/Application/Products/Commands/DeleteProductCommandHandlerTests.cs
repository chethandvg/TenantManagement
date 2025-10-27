using Archu.Application.Products.Commands.DeleteProduct;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Builders;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class DeleteProductCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenProductExists_DeletesProductSuccessfully(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithOwnerId(userId)
            .Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        fixture.VerifyProductDeleted();
        fixture.VerifySaveChangesCalled();
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenProductNotFound_ReturnsFailure(Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException(Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_CallsDeleteAsyncWithCorrectProduct(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        Product? deletedProduct = null;

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => deletedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        deletedProduct.Should().NotBeNull();
        deletedProduct.Should().BeSameAs(existingProduct);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(Guid userId, Guid productId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId);

        fixture.MockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        fixture.MockUnitOfWork.Setup(u => u.Products).Returns(fixture.MockProductRepository.Object);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenDeleteFails_ThrowsException(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_DoesNotCallDeleteAsync_WhenProductNotFound(Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.MockProductRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException(
        string invalidUserId,
        Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory]
    [InlineAutoMoqData("")]
    [InlineAutoMoqData("   ")]
    [InlineAutoMoqData("invalid-guid-format")]
    [InlineAutoMoqData("12345")]
    [InlineAutoMoqData("not-a-guid-at-all")]
    [InlineAutoMoqData("GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG")]
    public async Task Handle_WhenUserIdHasInvalidFormat_ThrowsUnauthorizedAccessException(
        string invalidUserId,
        Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory]
    [InlineAutoMoqData("")]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException(
        string emptyUserId,
        Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat(emptyUserId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenDeletingProduct(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} deleting product with ID: {existingProduct.Id}");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductDeleted(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product with ID {existingProduct.Id} deleted successfully");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenProductNotFound(Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsError_WhenUserNotAuthenticated(Guid productId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(productId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyErrorLogged("Cannot perform delete products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesUserIdInLogs(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged(userId.ToString(), Times.Exactly(2));
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesProductIdInAllLogs(Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();
        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.MockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(existingProduct.Id.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
