# TentMan Password Security Guide

Complete guide to password policies, complexity validation, and security configuration in TentMan.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Password Policy Configuration](#password-policy-configuration)
- [Password Complexity Rules](#password-complexity-rules)
- [Implementation](#implementation)
- [Testing](#testing)
- [Best Practices](#best-practices)

---

## 🎯 Overview

### Password Security Features

TentMan implements comprehensive password security:
- ✅ Configurable complexity requirements
- ✅ Minimum length enforcement
- ✅ Character type requirements (uppercase, lowercase, digits, symbols)
- ✅ Common password detection (top 100 passwords blocked)
- ✅ Username/email prevention in passwords
- ✅ Password strength scoring (0-100)
- ✅ Real-time validation feedback
- ✅ Environment-specific policies

---

## 🔧 Password Policy Configuration

### ASP.NET Core Identity Options

**Configuration** (`Program.cs`):
```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
});
```

### Environment-Specific Policies

**Development** (`appsettings.Development.json`):
```json
{
  "PasswordPolicy": {
    "RequiredLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireNonAlphanumeric": true,
    "RequiredUniqueChars": 4,
    "PreventCommonPasswords": true,
    "PreventUsernameInPassword": true
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "PasswordPolicy": {
    "RequiredLength": 12,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireNonAlphanumeric": true,
    "RequiredUniqueChars": 6,
    "PreventCommonPasswords": true,
    "PreventUsernameInPassword": true
  }
}
```

---

## 🔐 Password Complexity Rules

### Basic Requirements

| Rule | Development | Production |
|------|-------------|------------|
| Minimum Length | 8 characters | 12 characters |
| Uppercase Letters | Required | Required |
| Lowercase Letters | Required | Required |
| Digits (0-9) | Required | Required |
| Special Characters | Required | Required |
| Unique Characters | 4 minimum | 6 minimum |

### Advanced Security

✅ **Common Password Detection**
- Blocks top 100 most common passwords
- Examples: "password", "123456", "qwerty"

✅ **Username Prevention**
- Password cannot contain username
- Case-insensitive check

✅ **Email Prevention**
- Password cannot contain email address
- Checks both full email and local part

### Password Strength Scoring

Passwords are scored from 0-100:

| Score | Rating | Description |
|-------|--------|-------------|
| 0-20 | Very Weak | Easily guessable |
| 21-40 | Weak | Basic requirements only |
| 41-60 | Fair | Meets most requirements |
| 61-80 | Strong | Good complexity |
| 81-100 | Very Strong | Excellent security |

**Scoring Factors**:
- Length (longer is better)
- Character variety (uppercase, lowercase, digits, symbols)
- Unique character count
- Entropy (randomness)

---

## 🏗️ Implementation

### Password Validator Interface

```csharp
public interface IPasswordValidator
{
    Task<PasswordValidationResult> ValidateAsync(
        string password, 
        string? username = null, 
        string? email = null);
    
    int CalculateStrength(string password);
}
```

### Password Validation Result

```csharp
public class PasswordValidationResult
{
    public bool IsValid { get; init; }
    public int Strength { get; init; } // 0-100
    public List<string> Errors { get; init; } = new();
    
    public static PasswordValidationResult Success(int strength) => 
        new() { IsValid = true, Strength = strength };
    
    public static PasswordValidationResult Failure(params string[] errors) => 
        new() { IsValid = false, Errors = errors.ToList() };
}
```

### Password Validator Implementation

```csharp
public class PasswordValidator : IPasswordValidator
{
    private readonly PasswordPolicyOptions _options;
    private static readonly HashSet<string> CommonPasswords = new()
    {
        "password", "123456", "qwerty", "admin", "welcome",
        // ... top 100 common passwords
    };

    public async Task<PasswordValidationResult> ValidateAsync(
        string password, 
        string? username = null, 
        string? email = null)
    {
        var errors = new List<string>();

        // Length check
        if (password.Length < _options.RequiredLength)
            errors.Add($"Password must be at least {_options.RequiredLength} characters");

        // Complexity checks
        if (_options.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (_options.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (_options.RequireDigit && !password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit");

        if (_options.RequireNonAlphanumeric && !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Password must contain at least one special character");

        // Unique characters check
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < _options.RequiredUniqueChars)
            errors.Add($"Password must contain at least {_options.RequiredUniqueChars} unique characters");

        // Common password check
        if (_options.PreventCommonPasswords && 
            CommonPasswords.Contains(password.ToLowerInvariant()))
            errors.Add("This password is too common. Please choose a more secure password");

        // Username in password check
        if (_options.PreventUsernameInPassword && 
            !string.IsNullOrEmpty(username) &&
            password.Contains(username, StringComparison.OrdinalIgnoreCase))
            errors.Add("Password cannot contain your username");

        // Email in password check
        if (_options.PreventUsernameInPassword && 
            !string.IsNullOrEmpty(email) &&
            password.Contains(email, StringComparison.OrdinalIgnoreCase))
            errors.Add("Password cannot contain your email address");

        if (errors.Any())
            return PasswordValidationResult.Failure(errors.ToArray());

        var strength = CalculateStrength(password);
        return PasswordValidationResult.Success(strength);
    }

    public int CalculateStrength(string password)
    {
        var strength = 0;

        // Length scoring (0-40 points)
        strength += Math.Min(password.Length * 2, 40);

        // Complexity scoring (0-40 points)
        if (password.Any(char.IsUpper)) strength += 10;
        if (password.Any(char.IsLower)) strength += 10;
        if (password.Any(char.IsDigit)) strength += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) strength += 10;

        // Unique characters (0-20 points)
        var uniqueChars = password.Distinct().Count();
        strength += Math.Min(uniqueChars * 2, 20);

        return Math.Min(strength, 100);
    }
}
```

### FluentValidation Integration

**Register Command Validator**:
```csharp
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IPasswordValidator _passwordValidator;

    public RegisterCommandValidator(IPasswordValidator passwordValidator)
    {
        _passwordValidator = passwordValidator;

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MustAsync(async (command, password, ct) =>
            {
                var result = await _passwordValidator.ValidateAsync(
                    password, command.UserName, command.Email);
                return result.IsValid;
            })
            .WithMessage("Password does not meet complexity requirements");
    }
}
```

---

## 🧪 Testing

### Valid Passwords

✅ Development (8+ chars):
- `Test@123`
- `Secure$Pass2024`
- `MyP@ssw0rd`

✅ Production (12+ chars):
- `Test@1234567`
- `MySecureP@ssw0rd2024`
- `Str0ng!P@ssword`

### Invalid Passwords

❌ Too short:
- `Test@12` (< 8 chars)

❌ Missing complexity:
- `testpass` (no uppercase, digit, symbol)
- `TESTPASS` (no lowercase, digit, symbol)
- `Test1234` (no symbol)

❌ Common passwords:
- `password`
- `123456`
- `qwerty`

❌ Contains username/email:
- Username: `johndoe`, Password: `JohnDoe@123` ❌
- Email: `john@example.com`, Password: `john@Pass123` ❌

### Testing via API

**Test Registration with Weak Password**:
```http
POST /api/v1/authentication/register
Content-Type: application/json

{
  "userName": "testuser",
  "email": "test@example.com",
  "password": "weak"
}

Response 400 Bad Request:
{
  "success": false,
  "errors": [
    "Password must be at least 8 characters",
    "Password must contain at least one uppercase letter",
    "Password must contain at least one digit",
    "Password must contain at least one special character"
  ]
}
```

**Test Registration with Strong Password**:
```http
POST /api/v1/authentication/register
Content-Type: application/json

{
  "userName": "testuser",
  "email": "test@example.com",
  "password": "SecureP@ss123"
}

Response 200 OK:
{
  "accessToken": "...",
  "refreshToken": "...",
  "strength": 85
}
```

---

## ✅ Best Practices

### Password Policy

✅ **DO**:
- Enforce minimum 8 characters (12+ for production)
- Require multiple character types
- Block common passwords
- Prevent username/email in password
- Use environment-specific policies
- Educate users about password security

❌ **DON'T**:
- Allow weak passwords in production
- Make requirements too complex (user frustration)
- Store passwords in plaintext
- Log passwords (even in debug mode)

### Implementation

✅ **DO**:
- Validate passwords on both client and server
- Provide real-time feedback
- Use ASP.NET Core Identity password hasher
- Implement account lockout
- Log failed password attempts
- Allow password strength indicator

❌ **DON'T**:
- Rely on client-side validation only
- Skip validation for admin-created users
- Use custom/weak hashing algorithms
- Allow unlimited password attempts

### User Experience

✅ **DO**:
- Show password requirements upfront
- Provide helpful error messages
- Display password strength meter
- Allow password visibility toggle
- Support password managers

❌ **DON'T**:
- Show cryptic error messages
- Force frequent password changes (causes weak passwords)
- Prevent copy/paste (breaks password managers)

---

## 📚 Related Documentation

- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - Authentication system
- **[AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)** - Authorization
- **[API_GUIDE.md](API_GUIDE.md)** - API endpoints
- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
