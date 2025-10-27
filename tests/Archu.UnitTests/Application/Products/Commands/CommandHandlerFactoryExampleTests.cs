using Archu.Application.Products.Commands.CreateProduct;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Application.Products.Commands;

/// <summary>
/// Demonstrates using the CommandHandlerTestFixture.CreateHandler() factory method
/// to reduce test boilerplate and improve maintainability.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Pattern", "HandlerFactory")]
public class CommandHandlerFactoryExampleTests
{
    [Fact]
    public async Task CreateHandler_WithStandardConstructor_CreatesHandlerAutomatically()
    {
        // Arrange - Notice how we don't need to manually construct the handler
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        // The CreateHandler() method automatically creates the handler with the right dependencies
        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        fixture.VerifyProductAdded();
    }

    [Fact]
    public async Task CreateHandler_WithFluentConfiguration_ReducesBoilerplate()
    {
        // Arrange - Fluent configuration with automatic handler creation
        var userId = Guid.NewGuid();
        
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        // Single line to create handler with all configured mocks
        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand("Premium Product", 199.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(199.99m);
        fixture.VerifyInformationLogged($"User {userId} creating product: Premium Product");
    }

    [Fact]
    public void CreateHandler_WithCustomFactory_SupportsNonStandardConstructors()
    {
        // Arrange - Example showing custom factory for handlers with additional dependencies
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd()
            .WithHandlerFactory((unitOfWork, currentUser, logger) =>
            {
                // Custom construction logic can be added here
                // This is useful when you need to:
                // - Add additional dependencies
                // - Use a specific constructor overload
                // - Apply custom initialization logic
                return new CreateProductCommandHandler(unitOfWork, currentUser, logger);
            });

        // Act
        var handler = fixture.CreateHandler();

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleTests_UsingFactoryPattern_ShowConsistency()
    {
        // Demonstrates how the factory pattern ensures consistent handler creation across tests
        var testCases = new[]
        {
            ("Budget Item", 9.99m),
            ("Standard Item", 49.99m),
            ("Premium Item", 99.99m)
        };

        foreach (var (name, price) in testCases)
        {
            // Arrange - Same pattern for every test case
            var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
                .WithAuthenticatedUser()
                .WithProductRepositoryForAdd();

            var handler = fixture.CreateHandler();
            var command = new CreateProductCommand(name, price);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(name);
            result.Price.Should().Be(price);
        }
    }
}
