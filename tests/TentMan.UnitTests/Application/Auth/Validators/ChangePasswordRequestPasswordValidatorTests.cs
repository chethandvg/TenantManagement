using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Auth.Validators;
using TentMan.Contracts.Authentication;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Auth.Validators;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ChangePasswordRequestPasswordValidatorTests
{
    private readonly Mock<IPasswordValidator> _passwordValidatorMock;
    private readonly ChangePasswordRequestPasswordValidator _validator;

    public ChangePasswordRequestPasswordValidatorTests()
    {
        _passwordValidatorMock = CreatePasswordValidatorMockReturning(
            PasswordValidationResult.Success(85, PasswordStrengthLevel.Strong));
        _validator = new ChangePasswordRequestPasswordValidator(_passwordValidatorMock.Object);
    }

    [Fact]
    public void Validate_WhenRequestIsValid_PassesValidation()
    {
        // Arrange
        var request = CreateValidChangePasswordRequest();

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
    public void Validate_WhenCurrentPasswordIsNullOrWhitespace_FailsValidation(string? currentPassword)
    {
        // Arrange
        var request = CreateValidChangePasswordRequest() with { CurrentPassword = currentPassword ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ChangePasswordRequest.CurrentPassword) &&
            error.ErrorMessage == "Current password is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenNewPasswordIsNullOrEmpty_FailsValidation(string? newPassword)
    {
        // Arrange
        var request = CreateValidChangePasswordRequest() with { NewPassword = newPassword ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ChangePasswordRequest.NewPassword) &&
            error.ErrorMessage == "New password is required");
    }

    [Fact]
    public void Validate_WhenNewPasswordMatchesCurrentPassword_FailsValidation()
    {
        // Arrange
        var password = "CurrentPassword123!";
        var request = CreateValidChangePasswordRequest() with
        {
            CurrentPassword = password,
            NewPassword = password
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ChangePasswordRequest.NewPassword) &&
            error.ErrorMessage == "New password must be different from current password");
    }

    [Fact]
    public void Validate_WhenPasswordValidatorReturnsErrors_FailsValidationWithMessages()
    {
        // Arrange
        var passwordErrors = new[]
        {
            "Password must include a lowercase letter",
            "Password must include a symbol"
        };
        _passwordValidatorMock
            .Setup(validator => validator.ValidatePassword(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(PasswordValidationResult.Failure(passwordErrors));
        var request = CreateValidChangePasswordRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(passwordErrors.Length);
        foreach (var passwordError in passwordErrors)
        {
            result.Errors.Should().Contain(error =>
                error.PropertyName == nameof(ChangePasswordRequest.NewPassword) &&
                error.ErrorMessage == passwordError);
        }
    }

    /// <summary>
    /// Creates a password validator mock configured to return a predefined result.
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
    /// Provides a change password request that satisfies all validation rules for reuse in tests.
    /// </summary>
    private static ChangePasswordRequest CreateValidChangePasswordRequest()
    {
        return new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPass123!",
            NewPassword = "NewPass456!"
        };
    }
}
