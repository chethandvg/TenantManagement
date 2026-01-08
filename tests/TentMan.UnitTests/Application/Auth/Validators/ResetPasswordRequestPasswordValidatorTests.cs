using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Auth.Validators;
using TentMan.Contracts.Authentication;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Auth.Validators;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ResetPasswordRequestPasswordValidatorTests
{
    private readonly Mock<IPasswordValidator> _passwordValidatorMock;
    private readonly ResetPasswordRequestPasswordValidator _validator;

    public ResetPasswordRequestPasswordValidatorTests()
    {
        _passwordValidatorMock = CreatePasswordValidatorMockReturning(
            PasswordValidationResult.Success(90, PasswordStrengthLevel.VeryStrong));
        _validator = new ResetPasswordRequestPasswordValidator(_passwordValidatorMock.Object);
    }

    [Fact]
    public void Validate_WhenRequestIsValid_PassesValidation()
    {
        // Arrange
        var request = CreateValidResetPasswordRequest();

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
        var request = CreateValidResetPasswordRequest() with { Email = email ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ResetPasswordRequest.Email) &&
            error.ErrorMessage == "Email is required");
    }

    [Fact]
    public void Validate_WhenEmailFormatIsInvalid_FailsValidation()
    {
        // Arrange
        var request = CreateValidResetPasswordRequest() with { Email = "invalid-email" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ResetPasswordRequest.Email) &&
            error.ErrorMessage == "Invalid email address format");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenResetTokenIsNullOrWhitespace_FailsValidation(string? resetToken)
    {
        // Arrange
        var request = CreateValidResetPasswordRequest() with { ResetToken = resetToken ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ResetPasswordRequest.ResetToken) &&
            error.ErrorMessage == "Reset token is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenNewPasswordIsNullOrEmpty_FailsValidation(string? password)
    {
        // Arrange
        var request = CreateValidResetPasswordRequest() with { NewPassword = password ?? null! };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(ResetPasswordRequest.NewPassword) &&
            error.ErrorMessage == "New password is required");
    }

    [Fact]
    public void Validate_WhenPasswordValidatorReturnsErrors_FailsValidationWithMessages()
    {
        // Arrange
        var passwordErrors = new[]
        {
            "Password must be at least eight characters",
            "Password must not contain the email address"
        };
        _passwordValidatorMock
            .Setup(validator => validator.ValidatePassword(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(PasswordValidationResult.Failure(passwordErrors));
        var request = CreateValidResetPasswordRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(passwordErrors.Length);
        foreach (var passwordError in passwordErrors)
        {
            result.Errors.Should().Contain(error =>
                error.PropertyName == nameof(ResetPasswordRequest.NewPassword) &&
                error.ErrorMessage == passwordError);
        }
    }

    /// <summary>
    /// Creates a password validator mock that returns the supplied result for password checks.
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
    /// Constructs a reset password request that satisfies all validation rules for reuse.
    /// </summary>
    private static ResetPasswordRequest CreateValidResetPasswordRequest()
    {
        return new ResetPasswordRequest
        {
            Email = "user@example.com",
            ResetToken = "token-123",
            NewPassword = "ResetPass789!"
        };
    }
}
