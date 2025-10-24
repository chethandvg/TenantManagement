using Archu.Application.Products.Commands.UpdateProduct;
using Archu.Application.Products.Validators;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Application.Products.Validators;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator;

    public UpdateProductCommandValidatorTests()
    {
        _validator = new UpdateProductCommandValidator();
    }

    [Fact]
    public void Validate_WhenCommandIsValid_PassesValidation()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Product Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_FailsValidation()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.Empty,
            "Valid Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateProductCommand.Id));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Product ID is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsNullOrWhitespace_FailsValidation(string? name)
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            name!,
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateProductCommand.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Product name is required");
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var longName = new string('a', 201);
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            longName,
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateProductCommand.Name));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Product name cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_WhenNameIsExactlyMaxLength_PassesValidation()
    {
        // Arrange
        var name = new string('a', 200);
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            name,
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

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
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Name",
            price,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateProductCommand.Price));
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
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Name",
            price,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

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
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Name",
            price,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateProductCommand.Price));
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
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Valid Name",
            price,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenMultipleFieldsAreInvalid_ReturnsAllErrors()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.Empty,
            "",
            -10m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(2);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Id));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Price));
    }

    [Fact]
    public void Validate_WhenAllFieldsAreValid_PassesValidation()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var validName = "Test Product";
        var validPrice = 49.99m;
        var validRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var command = new UpdateProductCommand(validId, validName, validPrice, validRowVersion);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
