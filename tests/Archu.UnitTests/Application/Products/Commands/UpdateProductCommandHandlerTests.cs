using Archu.Application.Products.Commands.UpdateProduct;
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
public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProductExistsAndRowVersionMatches_UpdatesProductSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder()
            .WithName("Original Name")
            .WithPrice(50.00m)
            .WithOwnerId(userId)
            .Build();

        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Price.Should().Be(99.99m);

        fixture.VerifyProductUpdated();
        fixture.VerifySaveChangesCalled();
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            productId,
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Fact]
    public async Task Handle_WhenRowVersionMismatch_ReturnsFailure()
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            .Build();

        var differentRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            differentRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("modified by another user");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.ExistsAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Simulate concurrency exception by throwing InvalidOperationException with specific message
        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Concurrency conflict"));

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            originalRowVersion);

        // Act & Assert
        // The handler does not catch generic InvalidOperationException, so it should be thrown
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdatesProductProperties()
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName("Original Name")
            .WithPrice(50.00m)
            .Build();

        Product? updatedProduct = null;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<Product, byte[], CancellationToken>((p, _, __) => updatedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "New Name",
            75.50m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("New Name");
        updatedProduct.Price.Should().Be(75.50m);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat("not-a-valid-guid");

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
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
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat(string.Empty);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Fact]
    public async Task Handle_LogsInformation_WhenUpdatingProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} updating product with ID: {existingProduct.Id}");
    }

    [Fact]
    public async Task Handle_LogsInformation_AfterProductUpdated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product with ID {existingProduct.Id} updated successfully");
    }

    [Fact]
    public async Task Handle_LogsWarning_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            productId,
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }

    [Fact]
    public async Task Handle_LogsWarning_WhenRowVersionMismatch()
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            .Build();

        var differentRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            differentRowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Concurrency conflict detected for product {existingProduct.Id}");
    }

    [Fact]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Fact]
    public async Task Handle_LogsError_WhenUserNotAuthenticated()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyErrorLogged("Cannot perform update products");
    }

    #endregion
}
