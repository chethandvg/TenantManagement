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
    [Theory, AutoMoqData]
    public async Task Handle_WhenProductExistsAndRowVersionMatches_UpdatesProductSuccessfully(
        string originalName,
        decimal originalPrice,
        string updatedName,
        decimal updatedPrice,
        Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName(originalName)
            .WithPrice(originalPrice)
            .WithOwnerId(userId)
            .Build();

        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(updatedName);
        result.Value.Price.Should().Be(updatedPrice);

        fixture.VerifyProductUpdated();
        fixture.VerifySaveChangesCalled();
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenProductNotFound_ReturnsFailure(
        string productName,
        decimal price,
        Guid productId,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRowVersionMismatch_ReturnsFailure(
        string updatedName,
        decimal updatedPrice,
        byte[] originalRowVersion,
        byte[] differentRowVersion)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(originalRowVersion)
            .Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            differentRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("modified by another user");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException(
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionOccurs_ReturnsFailure(
        string updatedName,
        decimal updatedPrice)
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

        // Use the test-only DbUpdateConcurrencyException to exercise the catch block
        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestHelpers.Exceptions.DbUpdateConcurrencyException("Concurrency conflict"));

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("modified by another user");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionOccursAndProductStillExists_ReturnsModifiedByAnotherUserError(
        string updatedName,
        decimal updatedPrice)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        // Product still exists after concurrency exception
        fixture.MockProductRepository
            .Setup(r => r.ExistsAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestHelpers.Exceptions.DbUpdateConcurrencyException("Race condition detected"));

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("The product was modified by another user. Please refresh and try again.");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionOccursAndProductWasDeleted_ReturnsProductNotFoundError(
        string updatedName,
        decimal updatedPrice)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        // Product was deleted during update operation (race condition)
        fixture.MockProductRepository
            .Setup(r => r.ExistsAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestHelpers.Exceptions.DbUpdateConcurrencyException("Product was deleted"));

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionAndProductDeleted_LogsWarning(
        string updatedName,
        decimal updatedPrice)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var originalRowVersion = existingProduct.RowVersion;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.ExistsAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestHelpers.Exceptions.DbUpdateConcurrencyException());

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Product with ID {existingProduct.Id} was deleted during update operation");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionAndProductStillExists_LogsRaceConditionWarning(
        string updatedName,
        decimal updatedPrice)
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

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestHelpers.Exceptions.DbUpdateConcurrencyException());

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            originalRowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Race condition detected: Product {existingProduct.Id} was modified between validation and save");
    }

    [Theory, AutoMoqData]
    public async Task Handle_UpdatesProductProperties(
        string originalName,
        decimal originalPrice,
        string newName,
        decimal newPrice)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName(originalName)
            .WithPrice(originalPrice)
            .Build();

        Product? updatedProduct = null;

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        fixture.MockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<Product, byte[], CancellationToken>((p, _, __) => updatedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            newName,
            newPrice,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be(newName);
        updatedProduct.Price.Should().Be(newPrice);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException(
        string invalidUserId,
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
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
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Theory]
    [InlineAutoMoqData("")]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException(
        string emptyUserId,
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithInvalidUserIdFormat(emptyUserId);

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenUpdatingProduct(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} updating product with ID: {existingProduct.Id}");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductUpdated(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product with ID {existingProduct.Id} updated successfully");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenProductNotFound(
        string productName,
        decimal price,
        Guid productId,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenRowVersionMismatch(
        string updatedName,
        decimal updatedPrice,
        byte[] originalRowVersion,
        byte[] differentRowVersion)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(originalRowVersion)
            .Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            updatedName,
            updatedPrice,
            differentRowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyWarningLogged($"Concurrency conflict detected for product {existingProduct.Id}");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(
        string productName,
        decimal price)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsError_WhenUserNotAuthenticated(
        Guid productId,
        string productName,
        decimal price,
        byte[] rowVersion)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new UpdateProductCommand(productId, productName, price, rowVersion);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyErrorLogged("Cannot perform update products");
    }

    #endregion

    #region Cancellation Token Flow Tests

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToGetByIdAsync(
        string productName,
        decimal price)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        fixture.VerifyProductFetchedWithToken(existingProduct.Id, cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToUpdateAsync(
        string productName,
        decimal price)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        fixture.VerifyProductUpdatedWithToken(cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToSaveChangesAsync(
        string productName,
        decimal price)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var existingProduct = new ProductBuilder().Build();

        var fixture = new CommandHandlerTestFixture<UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = fixture.CreateHandler();

        var command = new UpdateProductCommand(
            existingProduct.Id,
            productName,
            price,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        fixture.VerifySaveChangesCalledWithToken(cancellationToken);
    }

    #endregion
}
