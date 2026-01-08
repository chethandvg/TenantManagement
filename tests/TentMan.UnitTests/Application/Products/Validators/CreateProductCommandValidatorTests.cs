using TentMan.Application.Products.Commands.CreateProduct;
using TentMan.Application.Products.Validators;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Application.Products.Validators;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _validator = new CreateProductCommandValidator();
    }

    [Fact]
    public void Validate_WhenCommandIsValid_PassesValidation()
    {
        // Arrange
        var command = new CreateProductCommand("Valid Product Name", 99.99m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsNullOrWhitespace_FailsValidation(string? name)
    {
        // Arrange
        var command = new CreateProductCommand(name!, 99.99m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateProductCommand.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Product name is required");
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longName = new string('a', 201);
        var command = new CreateProductCommand(longName, 99.99m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateProductCommand.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Product name cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_WhenNameIsExactlyMaxLength_PassesValidation()
    {
        // Arrange
        var name = new string('a', 200);
        var command = new CreateProductCommand(name, 99.99m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public void Validate_WhenPriceIsZeroOrNegative_FailsValidation(decimal price)
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", price);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateProductCommand.Price));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Price must be greater than zero");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(99.99)]
    [InlineData(1000.50)]
    public void Validate_WhenPriceIsPositive_PassesValidation(decimal price)
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", price);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(99.999)]
    [InlineData(100.123)]
    [InlineData(1.2345)]
    public void Validate_WhenPriceHasMoreThanTwoDecimalPlaces_FailsValidation(decimal price)
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", price);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateProductCommand.Price));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Price must contain at most two decimal places");
    }

    [Theory]
    [InlineData(99.99)]
    [InlineData(100.50)]
    [InlineData(1.5)]
    [InlineData(1.00)]
    public void Validate_WhenPriceHasTwoOrFewerDecimalPlaces_PassesValidation(decimal price)
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", price);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenMultipleFieldsAreInvalid_ReturnsAllErrors()
    {
        // Arrange
        var command = new CreateProductCommand("", -10m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
    }

    [Fact]
    public void Validate_WhenNameIsAtBoundary_PassesValidation()
    {
        // Arrange - Test with 1 character (minimum realistic length)
        var command = new CreateProductCommand("A", 99.99m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(9999999.99)]
    public void Validate_WhenPriceIsAtValidBoundaries_PassesValidation(decimal price)
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", price);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
