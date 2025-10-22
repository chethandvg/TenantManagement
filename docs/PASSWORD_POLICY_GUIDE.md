# Password Policy Guide

This guide explains how to configure and use the password policy feature to enforce secure password requirements in the Archu application.

---

## Table of Contents

1. [Overview](#overview)
2. [Configuration](#configuration)
3. [Password Requirements](#password-requirements)
4. [Validation](#validation)
5. [Integration](#integration)
6. [Customization](#customization)
7. [Security Best Practices](#security-best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Overview

The password policy system provides:
- ✅ **Configurable password complexity requirements**
- ✅ **Real-time password validation**
- ✅ **Password strength scoring (0-100)**
- ✅ **Common password detection**
- ✅ **Username/email prevention in passwords**
- ✅ **FluentValidation integration**
- ✅ **Environment-specific policies**

---

## Configuration

### appsettings.json Structure

```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 0,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MinimumLength` | `int` | `8` | Minimum password length (4-256) |
| `MaximumLength` | `int` | `128` | Maximum password length (MinimumLength-256) |
| `RequireUppercase` | `bool` | `true` | Require at least one uppercase letter (A-Z) |
| `RequireLowercase` | `bool` | `true` | Require at least one lowercase letter (a-z) |
| `RequireDigit` | `bool` | `true` | Require at least one digit (0-9) |
| `RequireSpecialCharacter` | `bool` | `true` | Require at least one special character |
| `MinimumUniqueCharacters` | `int` | `0` | Minimum unique characters (prevents "aaaa1234!A") |
| `PreventCommonPasswords` | `bool` | `true` | Prevent top 100 most common passwords |
| `PreventUserInfo` | `bool` | `true` | Prevent password containing username/email |
| `SpecialCharacters` | `string` | (see above) | Allowed special characters |

### Environment-Specific Configuration

#### Development (appsettings.Development.json)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 4,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true
  }
}
```

**Rationale**: Balanced security for development testing.

#### Staging (appsettings.Staging.json)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 10,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 6,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true
  }
}
```

**Rationale**: Stricter requirements for staging environment.

#### Production (appsettings.Production.json)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 12,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 8,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true
  }
}
```

**Rationale**: Maximum security for production.

---

## Password Requirements

### Minimum Requirements (Default Policy)

Your password must:
1. Be between 8 and 128 characters long
2. Contain at least one uppercase letter (A-Z)
3. Contain at least one lowercase letter (a-z)
4. Contain at least one digit (0-9)
5. Contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?~`)
6. Not be a commonly used password
7. Not contain your username or email

### Password Strength Levels

The system calculates password strength on a 0-100 scale:

| Score | Strength Level | Description |
|-------|----------------|-------------|
| 0-29 | Very Weak | Easily guessable |
| 30-49 | Weak | Vulnerable to attacks |
| 50-69 | Fair | Basic protection |
| 70-89 | Strong | Good protection |
| 90-100 | Very Strong | Excellent protection |

**Strength Calculation Factors:**
- **Length** (max 30 points): +10 for 8+, +10 for 12+, +10 for 16+ chars
- **Character Variety** (max 40 points): +10 each for lowercase, uppercase, digits, special chars
- **Complexity** (max 30 points): +10 for 6+ unique chars, +10 for 10+, +10 for 15+

---

## Validation

### Using IPasswordValidator

```csharp
public class UserRegistrationService
{
    private readonly IPasswordValidator _passwordValidator;

    public UserRegistrationService(IPasswordValidator passwordValidator)
    {
        _passwordValidator = passwordValidator;
    }

    public async Task<Result> RegisterUserAsync(string email, string username, string password)
    {
        // Validate password
        var validationResult = _passwordValidator.ValidatePassword(
            password, 
            username, 
            email);

        if (!validationResult.IsValid)
        {
            return Result.Failure(string.Join(", ", validationResult.Errors));
        }

        // Check password strength
        if (validationResult.StrengthLevel < PasswordStrengthLevel.Fair)
        {
            return Result.Failure($"Password is too weak. Strength: {validationResult.StrengthLevel}");
        }

        // Password is valid and strong enough
        // ... proceed with registration
    }
}
```

### FluentValidation Integration

The system includes pre-built validators:

#### RegisterRequestPasswordValidator
```csharp
using Archu.Application.Auth.Validators;

public class RegisterCommand : IRequest<Result>
{
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

// Automatically validated by FluentValidation pipeline
// No manual validation needed!
```

#### ChangePasswordRequestPasswordValidator
```csharp
public class ChangePasswordCommand : IRequest<Result>
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

// Automatically validated
// Ensures new password != current password
```

#### ResetPasswordRequestPasswordValidator
```csharp
public class ResetPasswordCommand : IRequest<Result>
{
    public string Email { get; set; }
    public string ResetToken { get; set; }
    public string NewPassword { get; set; }
}

// Automatically validated
// Prevents password containing email
```

---

## Integration

### MediatR Pipeline

Password validation is automatically applied through the FluentValidation behavior:

```csharp
// In Program.cs (already configured)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // ← Validates passwords
});
```

### Dependency Injection

The password validator is automatically registered:

```csharp
// In DependencyInjection.cs (already configured)
services.AddScoped<IPasswordValidator, PasswordValidator>();
services.Configure<PasswordPolicyOptions>(options =>
{
    configuration.GetSection(PasswordPolicyOptions.SectionName).Bind(options);
    options.Validate();
});
```

### API Controller Example

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Password is automatically validated by FluentValidation pipeline
        var command = new RegisterCommand
        {
            Email = request.Email,
            Username = request.UserName,
            Password = request.Password
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            // Returns 400 Bad Request with validation errors
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Value);
    }
}
```

---

## Customization

### Adding Custom Password Rules

To add custom password validation rules:

```csharp
public class CustomPasswordValidator : IPasswordValidator
{
    private readonly PasswordValidator _baseValidator;
    private readonly MyCustomOptions _options;

    public CustomPasswordValidator(
        PasswordValidator baseValidator,
        IOptions<MyCustomOptions> options)
    {
        _baseValidator = baseValidator;
        _options = options.Value;
    }

    public PasswordValidationResult ValidatePassword(
        string password, 
        string? username = null, 
        string? email = null)
    {
        // Call base validation first
        var result = _baseValidator.ValidatePassword(password, username, email);

        if (!result.IsValid)
        {
            return result;
        }

        var errors = new List<string>();

        // Add custom rules
        if (_options.PreventRepeatingCharacters && HasRepeatingCharacters(password))
        {
            errors.Add("Password cannot contain repeating characters (e.g., 'aaa', '111')");
        }

        if (_options.RequireMultipleSpecialChars && CountSpecialChars(password) < 2)
        {
            errors.Add("Password must contain at least 2 special characters");
        }

        if (errors.Any())
        {
            return PasswordValidationResult.Failure(errors);
        }

        return result;
    }

    private bool HasRepeatingCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }
}
```

### Extending Common Passwords List

To add more common passwords:

```csharp
// In PasswordValidator.cs
private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
{
    // Existing passwords...
    "password", "123456", "qwerty",
    
    // Add your custom common passwords
    "mycompany2024",
    "companyname",
    "productname"
};
```

### Custom Special Characters

```json
{
  "PasswordPolicy": {
    "SpecialCharacters": "!@#$%^&*()-_+=<>?"
  }
}
```

---

## Security Best Practices

### 1. Environment-Specific Policies

✅ **DO**: Use stricter policies in production
```json
// Production
{
  "MinimumLength": 12,
  "MinimumUniqueCharacters": 8
}

// Development
{
  "MinimumLength": 8,
  "MinimumUniqueCharacters": 4
}
```

### 2. Common Password Prevention

✅ **DO**: Always enable common password prevention
```json
{
  "PreventCommonPasswords": true
}
```

The system includes the top 100 most common passwords:
- password, 123456, qwerty, admin, letmein, etc.

### 3. Username/Email Prevention

✅ **DO**: Prevent passwords containing user information
```json
{
  "PreventUserInfo": true
}
```

This prevents:
- Password: "john123" with username: "john"
- Password: "admin@test" with email: "admin@test.com"

### 4. Minimum Unique Characters

✅ **DO**: Require diverse character usage
```json
{
  "MinimumUniqueCharacters": 6
}
```

Prevents weak passwords like:
- ❌ "aaaa1234!A" (only 5 unique chars)
- ✅ "aB3!xY9@" (8 unique chars)

### 5. Password Strength Feedback

✅ **DO**: Show strength indicators to users
```csharp
var result = _passwordValidator.ValidatePassword(password);

Console.WriteLine($"Strength: {result.StrengthLevel} ({result.StrengthScore}/100)");

// Output: Strength: Strong (82/100)
```

### 6. Password History (Future Enhancement)

🔜 **TODO**: Implement password history
```csharp
// Prevent reusing last 5 passwords
services.AddPasswordHistory(options =>
{
    options.HistorySize = 5;
    options.PreventReuse = true;
});
```

---

## Troubleshooting

### Password Always Fails Validation

**Problem**: All passwords fail validation even if they meet requirements.

**Solution**: Check configuration is loaded correctly:
```csharp
var policy = configuration.GetSection("PasswordPolicy").Get<PasswordPolicyOptions>();
Console.WriteLine($"MinLength: {policy.MinimumLength}");
Console.WriteLine($"RequireUpper: {policy.RequireUppercase}");
```

### "Password is too common" Error

**Problem**: Password is rejected as common even though it seems unique.

**Cause**: Password matches one of the 100 most common passwords (case-insensitive).

**Solution**: Use a more complex password:
- ❌ "password123"
- ❌ "admin123"
- ✅ "MyS3cur3P@ssw0rd!"

### Special Characters Not Recognized

**Problem**: Password with special characters still fails validation.

**Cause**: Character not in `SpecialCharacters` configuration.

**Solution**: Add the character to the configuration:
```json
{
  "PasswordPolicy": {
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`€£¥"
  }
}
```

### Configuration Not Applied

**Problem**: Changes to appsettings.json not reflected.

**Solutions**:
1. Restart the application
2. Check you're editing the correct environment file
3. Verify configuration section name matches `"PasswordPolicy"`
4. Check for validation errors on startup

### Validation Errors in Production

**Problem**: Passwords that worked in development fail in production.

**Cause**: Different policy settings per environment.

**Solution**: Check production configuration:
```bash
# Production may have stricter requirements
MinimumLength: 12 (vs 8 in dev)
MinimumUniqueCharacters: 8 (vs 4 in dev)
```

---

## API Error Responses

### Registration with Weak Password

**Request:**
```http
POST /api/v1/auth/register
{
  "email": "user@example.com",
  "username": "john",
  "password": "short"
}
```

**Response:**
```json
{
  "success": false,
  "errors": [
    "Password must be at least 8 characters long",
    "Password must contain at least one uppercase letter (A-Z)",
    "Password must contain at least one digit (0-9)",
    "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?~`)"
  ]
}
```

### Password Contains Username

**Request:**
```http
POST /api/v1/auth/register
{
  "email": "user@example.com",
  "username": "john",
  "password": "John123456!"
}
```

**Response:**
```json
{
  "success": false,
  "errors": [
    "Password cannot contain your username"
  ]
}
```

### Common Password

**Request:**
```http
POST /api/v1/auth/change-password
{
  "currentPassword": "OldP@ssw0rd!",
  "newPassword": "password123"
}
```

**Response:**
```json
{
  "success": false,
  "errors": [
    "Password is too common. Please choose a more secure password"
  ]
}
```

---

## Get Password Requirements

To display requirements to users:

```csharp
[HttpGet("password-requirements")]
public IActionResult GetPasswordRequirements(
    [FromServices] IPasswordValidator passwordValidator)
{
    var requirements = passwordValidator.GetPasswordRequirements();
    return Ok(new { requirements });
}
```

**Response:**
```json
{
  "requirements": "1. Be between 8 and 128 characters long\n2. Contain at least one uppercase letter (A-Z)\n3. Contain at least one lowercase letter (a-z)\n4. Contain at least one digit (0-9)\n5. Contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?~`)\n6. Contain at least 4 unique characters\n7. Not be a commonly used password\n8. Not contain your username or email"
}
```

---

## Related Documentation

| Document | Description |
|----------|-------------|
| **[Authentication Infrastructure](../src/Archu.Infrastructure/Authentication/README.md)** | Authentication components |
| **[Database Seeding Guide](DATABASE_SEEDING_GUIDE.md)** | Initial data setup |
| **[JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)** | JWT authentication |

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
