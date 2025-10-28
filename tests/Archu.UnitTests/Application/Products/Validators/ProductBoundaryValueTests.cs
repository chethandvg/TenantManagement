using Archu.Application.Products.Commands.CreateProduct;
using Archu.Application.Products.Commands.UpdateProduct;
using Archu.Application.Products.Validators;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Builders;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace Archu.UnitTests.Application.Products.Validators;

/// <summary>
/// Tests for boundary values and edge cases in Product domain validation.
/// These tests ensure that the system correctly handles extreme values, invalid inputs,
/// and edge cases that could cause issues in production.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Type", "BoundaryValue")]
public class ProductBoundaryValueTests
{
    #region Price Boundary Tests

    [Theory]
    [InlineData(0)]           // Zero - Invalid
    [InlineData(-0.01)]       // Negative - Invalid
    [InlineData(-1.00)]       // Negative - Invalid
    [InlineData(-999.99)]     // Large negative - Invalid
    public async Task CreateProduct_WithInvalidPrice_FailsValidation(decimal invalidPrice)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Test Product", invalidPrice);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than zero"));
    }

    [Theory]
    [InlineData(0.01)]        // Minimum valid price
    [InlineData(0.99)]        // Less than 1
    [InlineData(1.00)]        // Exactly 1
    [InlineData(9.99)]        // Standard price
    [InlineData(99.99)]       // Moderate price
    [InlineData(999.99)]      // High price
    [InlineData(9999.99)]     // Very high price
    [InlineData(99999.99)]    // Extremely high price
    public async Task CreateProduct_WithValidPrice_PassesValidation(decimal validPrice)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Test Product", validPrice);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Price))
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.001)]       // Three decimal places - Invalid
    [InlineData(0.999)]       // Three decimal places - Invalid
    [InlineData(1.001)]       // Three decimal places - Invalid
    [InlineData(9.999)]       // Three decimal places - Invalid
    [InlineData(99.999)]      // Three decimal places - Invalid
    [InlineData(1.1234)]      // Four decimal places - Invalid
    public async Task CreateProduct_WithInvalidDecimalPrecision_FailsValidation(decimal invalidPrice)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Test Product", invalidPrice);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateProductCommand.Price) &&
            e.ErrorMessage.Contains("two decimal places"));
    }

    [Theory]
    [InlineData(0.01)]        // Two decimal places - Valid
    [InlineData(0.10)]        // Two decimal places - Valid
    [InlineData(1.00)]        // Two decimal places - Valid
    [InlineData(9.99)]        // Two decimal places - Valid
    [InlineData(99.00)]       // Two decimal places - Valid
    [InlineData(100.50)]      // Two decimal places - Valid
    public async Task CreateProduct_WithValidDecimalPrecision_PassesValidation(decimal validPrice)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Test Product", validPrice);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Price))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithMaxDecimalValue_IsAllowedByValidator()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        // decimal.MaxValue = 79228162514264337593543950335 (no decimal places)
        var command = new CreateProductCommand("Test Product", decimal.MaxValue);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        // The validator only checks > 0 and decimal precision
        // decimal.MaxValue passes both checks (it's > 0 and has no decimal places)
        // In practice, this would likely fail at the database level due to column constraints
        result.IsValid.Should().BeTrue();
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Price))
            .Should().BeEmpty();
    }

    #endregion

    #region Name Boundary Tests

    [Theory]
    [InlineData("")]          // Empty string - Invalid
    [InlineData(" ")]         // Whitespace only - Invalid
    [InlineData("   ")]       // Multiple whitespaces - Invalid
    [InlineData("\t")]        // Tab character - Invalid
    [InlineData("\n")]        // Newline character - Invalid
    public async Task CreateProduct_WithInvalidName_FailsValidation(string invalidName)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(invalidName, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateProductCommand.Name) &&
            e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData("A")]                         // Single character - Valid
    [InlineData("AB")]                        // Two characters - Valid
    [InlineData("Test")]                      // Short name - Valid
    [InlineData("Product Name")]              // Normal name - Valid
    [InlineData("A Very Long Product Name")] // Longer name - Valid
    public async Task CreateProduct_WithValidName_PassesValidation(string validName)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(validName, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Name))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithMaxLengthName_PassesValidation()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var maxLengthName = new string('A', 200); // Exactly 200 characters
        var command = new CreateProductCommand(maxLengthName, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Name))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithNameExceedingMaxLength_FailsValidation()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var tooLongName = new string('A', 201); // 201 characters - exceeds limit
        var command = new CreateProductCommand(tooLongName, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateProductCommand.Name) &&
            e.ErrorMessage.Contains("200 characters"));
    }

    [Theory]
    [InlineData("Product™")]                   // Trademark symbol
    [InlineData("Café")]                       // Accented character
    [InlineData("Product-Name")]               // Hyphen
    [InlineData("Product_Name")]               // Underscore
    [InlineData("Product (Special)")]          // Parentheses
    [InlineData("Product & Service")]          // Ampersand
    [InlineData("100% Natural")]               // Percentage symbol
    [InlineData("Product #1")]                 // Hash symbol
    [InlineData("Product @ $99.99")]           // Special characters
    public async Task CreateProduct_WithSpecialCharactersInName_PassesValidation(string nameWithSpecialChars)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(nameWithSpecialChars, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Name))
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData("商品")]                        // Chinese characters
    [InlineData("製品")]                        // Japanese characters
    [InlineData("제품")]                        // Korean characters
    [InlineData("продукт")]                    // Cyrillic characters
    [InlineData("منتج")]                       // Arabic characters
    [InlineData("उत्पाद")]                     // Hindi characters
    public async Task CreateProduct_WithUnicodeCharactersInName_PassesValidation(string unicodeName)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(unicodeName, 99.99m);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.Errors.Where(e => e.PropertyName == nameof(CreateProductCommand.Name))
            .Should().BeEmpty();
    }

    #endregion

    #region RowVersion Boundary Tests (Update Command)

    [Fact]
    public async Task UpdateProduct_WithEmptyRowVersion_FailsValidation()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            99.99m,
            Array.Empty<byte>());

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateProductCommand.RowVersion) &&
            e.ErrorMessage.Contains("least one byte"));
    }

    [Fact]
    public async Task UpdateProduct_WithNullRowVersion_FailsValidation()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            99.99m,
            null!);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateProductCommand.RowVersion) &&
            e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task UpdateProduct_WithValidRowVersion_PassesValidation()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]           // Single byte
    [InlineData(4)]           // Four bytes
    [InlineData(8)]           // Eight bytes (typical SQL Server rowversion)
    [InlineData(16)]          // Sixteen bytes
    [InlineData(32)]          // Thirty-two bytes
    public async Task UpdateProduct_WithVariousRowVersionLengths_PassesValidation(int byteCount)
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var rowVersion = Enumerable.Range(1, byteCount).Select(i => (byte)i).ToArray();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            99.99m,
            rowVersion);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ID Boundary Tests (Update Command)

    [Fact]
    public async Task UpdateProduct_WithEmptyGuid_FailsValidation()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.Empty,
            "Updated Product",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductCommand.Id));
    }

    [Fact]
    public async Task UpdateProduct_WithValidGuid_PassesValidation()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Domain Entity Boundary Tests

    [Fact]
    public void Product_WithMinimumValidValues_CreatesSuccessfully()
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithName("A")
            .WithPrice(0.01m)
            .Build();

        // Assert
        product.Name.Should().Be("A");
        product.Price.Should().Be(0.01m);
        product.Id.Should().NotBeEmpty();
        product.RowVersion.Should().NotBeEmpty();
    }

    [Fact]
    public void Product_WithMaximumNameLength_CreatesSuccessfully()
    {
        // Arrange
        var maxLengthName = new string('A', 200);

        // Act
        var product = new ProductBuilder()
            .WithName(maxLengthName)
            .WithPrice(99.99m)
            .Build();

        // Assert
        product.Name.Should().Be(maxLengthName);
        product.Name.Length.Should().Be(200);
    }

    [Fact]
    public void Product_WithLargePrice_StoresPrecisely()
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithName("Expensive Product")
            .WithPrice(99999.99m)
            .Build();

        // Assert
        product.Price.Should().Be(99999.99m);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(9.99)]
    [InlineData(99.99)]
    [InlineData(999.99)]
    public void Product_WithVariousPrices_StoresPrecisely(decimal price)
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithName("Test Product")
            .WithPrice(price)
            .Build();

        // Assert
        product.Price.Should().Be(price);
    }

    [Fact]
    public void Product_WithEmptyGuidOwnerId_IsValid()
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithOwnerId(Guid.Empty)
            .Build();

        // Assert
        product.OwnerId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Product_WithEmptyRowVersion_IsValid()
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithRowVersion(Array.Empty<byte>())
            .Build();

        // Assert
        product.RowVersion.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void Product_WithVariousRowVersionLengths_StoresCorrectly(int length)
    {
        // Arrange
        var rowVersion = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();

        // Act
        var product = new ProductBuilder()
            .WithRowVersion(rowVersion)
            .Build();

        // Assert
        product.RowVersion.Should().BeEquivalentTo(rowVersion);
        product.RowVersion.Length.Should().Be(length);
    }

    #endregion
}
