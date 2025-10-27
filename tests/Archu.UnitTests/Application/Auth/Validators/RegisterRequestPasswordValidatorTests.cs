using Archu.Application.Abstractions.Authentication;
using Archu.Application.Auth.Validators;
using Archu.Contracts.Authentication;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Validators;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class RegisterRequestPasswordValidatorTests
{
    private readonly Mock<IPasswordValidator> _passwordValidatorMock;
    private readonly RegisterRequestPasswordValidator _validator;

    public RegisterRequestPasswordValidatorTests()
    {
        _passwordValidatorMock = CreatePasswordValidatorMockReturning(
            PasswordValidationResult.Success(80, PasswordStrengthLevel.Strong));
        _validator = new RegisterRequestPasswordValidator(_passwordValidatorMock.Object);
    }

    [Fact]
    public void Validate_WhenRequestIsValid_PassesValidation()
    {
        // Arrange
        var request = CreateValidRegisterRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenEmailIsNullOrWhitespace_FailsValidation(string? email)
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { Email = email ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorMessage == "Email is required");
    }

    [Fact]
    public void Validate_WhenEmailFormatIsInvalid_FailsValidation()
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { Email = "invalid-email" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorMessage == "Invalid email address format");
    }

    [Fact]
    public void Validate_WhenEmailIsAtMaximumLength_PassesValidation()
    {
        // Arrange
        var maxEmail = $"{new string('a', 244)}@example.com";
        var request = CreateValidRegisterRequest() with { Email = maxEmail };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmailExceedsMaximumLength_FailsValidation()
    {
        // Arrange
        var tooLongEmail = $"{new string('a', 245)}@example.com";
        var request = CreateValidRegisterRequest() with { Email = tooLongEmail };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorMessage == "Email cannot exceed 256 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenUserNameIsNullOrWhitespace_FailsValidation(string? userName)
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = userName ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.UserName) &&
            error.ErrorMessage == "Username is required");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Validate_WhenUserNameIsTooShort_FailsValidation(string userName)
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = userName };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.UserName) &&
            error.ErrorMessage == "Username must be at least 3 characters");
    }

    [Fact]
    public void Validate_WhenUserNameIsAtMinimumLength_PassesValidation()
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = "abc" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUserNameIsAtMaximumLength_PassesValidation()
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = new string('a', 50) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUserNameExceedsMaximumLength_FailsValidation()
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = new string('a', 51) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.UserName) &&
            error.ErrorMessage == "Username cannot exceed 50 characters");
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("invalid@name")]
    [InlineData("name!" )]
    public void Validate_WhenUserNameContainsInvalidCharacters_FailsValidation(string userName)
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { UserName = userName };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.UserName) &&
            error.ErrorMessage == "Username can only contain letters, numbers, hyphens, and underscores");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenPasswordIsNullOrEmpty_FailsValidation(string? password)
    {
        // Arrange
        var request = CreateValidRegisterRequest() with { Password = password ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(RegisterRequest.Password) &&
            error.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_WhenPasswordValidatorReturnsError_FailsValidationWithMessages()
    {
        // Arrange
        var passwordErrors = new[]
        {
            "Password must contain an uppercase letter",
            "Password must contain a digit"
        };
        _passwordValidatorMock
            .Setup(validator => validator.ValidatePassword(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(PasswordValidationResult.Failure(passwordErrors));
        var request = CreateValidRegisterRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(passwordErrors.Length);
        foreach (var passwordError in passwordErrors)
        {
            result.Errors.Should().Contain(error =>
                error.PropertyName == nameof(RegisterRequest.Password) &&
                error.ErrorMessage == passwordError);
        }
    }

    /// <summary>
    /// Creates a password validator mock that returns the provided result for any password input.
    /// </summary>
    private static Mock<IPasswordValidator> CreatePasswordValidatorMockReturning(PasswordValidationResult result)
    {
        var passwordValidatorMock = new Mock<IPasswordValidator>();
        passwordValidatorMock
            .Setup(validator => validator.ValidatePassword(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(result);
        return passwordValidatorMock;
    }

    /// <summary>
    /// Builds a register request that satisfies all validation rules unless overridden in a test.
    /// </summary>
    private static RegisterRequest CreateValidRegisterRequest()
    {
        return new RegisterRequest
        {
            Email = "user@example.com",
            Password = "ValidPass123!",
            UserName = "valid_user"
        };
    }
}
