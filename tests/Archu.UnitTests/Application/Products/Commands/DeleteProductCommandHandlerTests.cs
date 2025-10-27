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
    [Fact]
    public async Task Handle_WhenProductExists_DeletesProductSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder()
            .WithOwnerId(userId)
            .Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        fixture.VerifyProductDeleted();
        fixture.VerifySaveChangesCalled();
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CallsDeleteAsyncWithCorrectProduct()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        Product? deletedProduct = null;

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => deletedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        deletedProduct.Should().NotBeNull();
        deletedProduct.Should().BeSameAs(existingProduct);
    }

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser();

        fixture.MockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        fixture.MockUnitOfWork.Setup(u => u.Products).Returns(fixture.MockProductRepository.Object);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_WhenDeleteFails_ThrowsException()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DoesNotCallDeleteAsync_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.MockProductRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat("not-a-valid-guid");

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-guid-format")]
    [InlineData("12345")]
    [InlineData("not-a-guid-at-all")]
    [InlineData("GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG")]
    public async Task Handle_WhenUserIdHasInvalidFormat_ThrowsUnauthorizedAccessException(
        string invalidUserId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithInvalidUserIdFormat(string.Empty);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Fact]
    public async Task Handle_LogsInformation_WhenDeletingProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} deleting product with ID: {existingProduct.Id}");
    }

    [Fact]
    public async Task Handle_LogsInformation_AfterProductDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product with ID {existingProduct.Id} deleted successfully");
    }

    [Fact]
    public async Task Handle_LogsWarning_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }

    [Fact]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Fact]
    public async Task Handle_LogsError_WhenUserNotAuthenticated()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyErrorLogged("Cannot perform delete products");
    }

    [Fact]
    public async Task Handle_IncludesUserIdInLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged(userId.ToString(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_IncludesProductIdInAllLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

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
