# Password Complexity Validation - Quick Start Guide

## 🚀 5-Minute Setup

This guide gets you up and running with password validation in **5 minutes**.

---

## Step 1: Configuration (2 minutes)

### Add to `appsettings.json`

```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true
  }
}
```

**That's it!** The password validator is already registered in DI.

---

## Step 2: Test It (3 minutes)

### Test Registration

```http
### Test with valid password
POST https://localhost:7123/api/v1/authentication/register
Content-Type: application/json

{
  "email": "test@example.com",
  "userName": "testuser",
  "password": "ValidP@ssw0rd123!"
}

### Expected: 201 Created ✅

### Test with weak password
POST https://localhost:7123/api/v1/authentication/register
Content-Type: application/json

{
  "email": "test@example.com",
  "userName": "testuser",
  "password": "weak"
}

### Expected: 400 Bad Request ❌
### Response:
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Password must be at least 8 characters long",
    "Password must contain at least one uppercase letter (A-Z)",
    "Password must contain at least one digit (0-9)",
    "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?~`)"
  ]
}
```

### Test Common Password Detection

```http
POST https://localhost:7123/api/v1/authentication/register
Content-Type: application/json

{
  "email": "test@example.com",
  "userName": "testuser",
  "password": "Password123!"
}

### Expected: 400 Bad Request ❌
### Response:
{
  "success": false,
  "errors": [
    "Password is too common. Please choose a more secure password"
  ]
}
```

### Test Username Containment

```http
POST https://localhost:7123/api/v1/authentication/register
Content-Type: application/json

{
  "email": "test@example.com",
  "userName": "johndoe",
  "password": "JohnDoe123!"
}

### Expected: 400 Bad Request ❌
### Response:
{
  "success": false,
  "errors": [
    "Password cannot contain your username"
  ]
}
```

---

## Done! ✅

Your password validation is now working. Users will receive clear, helpful error messages when their passwords don't meet requirements.

---

## Configuration Presets

### Development (Relaxed)

```json
{
  "PasswordPolicy": {
    "MinimumLength": 6,
    "RequireUppercase": false,
    "RequireSpecialCharacter": false,
    "PreventCommonPasswords": false
  }
}
```

### Production (Strict)

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

---

## Environment-Specific Configuration

### Option 1: Separate Files

**appsettings.Development.json:**
```json
{
  "PasswordPolicy": {
    "MinimumLength": 6,
    "RequireSpecialCharacter": false
  }
}
```

**appsettings.Production.json:**
```json
{
  "PasswordPolicy": {
    "MinimumLength": 12,
    "MinimumUniqueCharacters": 8
  }
}
```

### Option 2: Environment Variables

```bash
# Linux/macOS
export PasswordPolicy__MinimumLength=12
export PasswordPolicy__RequireSpecialCharacter=true

# Windows PowerShell
$env:PasswordPolicy__MinimumLength=12
$env:PasswordPolicy__RequireSpecialCharacter="true"
```

---

## Testing the Configuration

### View Current Requirements

```csharp
@inject IPasswordValidator PasswordValidator

<MudText>
    @PasswordValidator.GetPasswordRequirements()
</MudText>

// Output:
// Password must:
// 1. Be between 8 and 128 characters long
// 2. Contain at least one uppercase letter (A-Z)
// 3. Contain at least one lowercase letter (a-z)
// 4. Contain at least one digit (0-9)
// 5. Contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?~`)
// 6. Not be a commonly used password
// 7. Not contain your username or email
```

### Validate a Password

```csharp
@inject IPasswordValidator PasswordValidator

var result = PasswordValidator.ValidatePassword(
    password: "MyP@ssw0rd123!",
    username: "johndoe",
    email: "john@example.com"
);

if (result.IsValid)
{
    Console.WriteLine($"Password is {result.StrengthLevel}");
    Console.WriteLine($"Strength score: {result.StrengthScore}/100");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"❌ {error}");
    }
}
```

---

## Common Configuration Options

| Setting | Values | Recommended |
|---------|--------|-------------|
| `MinimumLength` | 4-256 | 8-12 |
| `MaximumLength` | MinLen-256 | 128 |
| `RequireUppercase` | true/false | true |
| `RequireLowercase` | true/false | true |
| `RequireDigit` | true/false | true |
| `RequireSpecialCharacter` | true/false | true |
| `MinimumUniqueCharacters` | 0-MinLen | 0 (or 8 for high security) |
| `PreventCommonPasswords` | true/false | true |
| `PreventUserInfo` | true/false | true |

---

## Password Strength Levels

| Score | Level | Example Password |
|-------|-------|------------------|
| 0-29 | VeryWeak | `password` |
| 30-49 | Weak | `Password1` |
| 50-69 | Fair | `P@ssw0rd!` |
| 70-89 | Strong | `MyP@ssw0rd123!` |
| 90-100 | VeryStrong | `C0mpl3x!P@ssw0rd#2024` |

---

## Troubleshooting

### Issue: Configuration not loading

**Check:**
1. JSON is valid (no trailing commas)
2. Section name is exactly `"PasswordPolicy"`
3. File is set to "Copy to Output Directory"
4. Application has been restarted

### Issue: Validation not working

**Check:**
1. `IPasswordValidator` is injected in validators
2. FluentValidation is registered in DI
3. `ValidationBehavior` is in MediatR pipeline
4. Application logs for validation errors

### Issue: Configuration validation error on startup

**Fix:**
```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,  // Must be >= 4
    "MaximumLength": 128, // Must be >= MinimumLength
    "MinimumUniqueCharacters": 0 // Must be <= MinimumLength
  }
}
```

---

## Next Steps

- **Read the full guide**: [PASSWORD_COMPLEXITY_VALIDATION.md](PASSWORD_COMPLEXITY_VALIDATION.md)
- **Configure security**: [SECURITY_CONFIGURATION.md](SECURITY_CONFIGURATION.md)
- **Review implementation**: [PASSWORD_VALIDATION_IMPLEMENTATION_SUMMARY.md](PASSWORD_VALIDATION_IMPLEMENTATION_SUMMARY.md)

---

## Quick Reference

### Valid Password Examples ✅

```
Strong8Pass!
MySecureP@ss123
C0mpl3x!Str0ng#2024
Valid&P@ssw0rd99
Secure!Passw0rd$
```

### Invalid Password Examples ❌

```
weak              // Too short, no uppercase, no digit, no special
Password123       // Common password
Password123!      // Common password variant
johndoe123!       // Contains username
john@example123!  // Contains email
12345678!A        // Too common pattern
```

---

**Time to complete**: 5 minutes ⏱️  
**Difficulty**: Easy 🟢  
**Status**: Production-Ready ✅

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
