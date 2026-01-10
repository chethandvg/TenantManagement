using TentMan.Application.Abstractions.Security;
using TentMan.Domain.Constants;

namespace TentMan.Infrastructure.Security;

/// <summary>
/// Implementation of data masking service for protecting sensitive information.
/// </summary>
public class DataMaskingService : IDataMaskingService
{
    private const string MaskCharacter = "*";
    private const int DefaultVisibleChars = 4;

    public string MaskDocumentNumber(string? value, int visibleChars = DefaultVisibleChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Validate visibleChars parameter
        if (visibleChars < 0 || visibleChars > value.Length)
        {
            visibleChars = Math.Min(DefaultVisibleChars, value.Length);
        }

        if (value.Length <= visibleChars)
        {
            return new string('*', value.Length);
        }

        var visiblePart = value.Substring(value.Length - visibleChars);
        var maskedPart = new string('*', 4); // Always show 4 asterisks
        return $"{maskedPart}{visiblePart}";
    }

    public string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return email; // Invalid email, return as-is
        }

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 1)
        {
            return $"{MaskCharacter}@{domain}";
        }

        var maskedLocal = localPart[0] + new string('*', Math.Min(3, localPart.Length - 1));
        return $"{maskedLocal}@{domain}";
    }

    public string MaskPhoneNumber(string? phone, int visibleChars = DefaultVisibleChars)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        // Remove non-digit characters for masking logic
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        // Validate visibleChars parameter
        if (visibleChars < 0 || visibleChars > digits.Length)
        {
            visibleChars = Math.Min(DefaultVisibleChars, digits.Length);
        }

        if (digits.Length <= visibleChars)
        {
            return phone; // Too short to mask effectively
        }

        // Keep country code and last N digits visible
        var prefix = phone.StartsWith("+") ? "+" : "";
        var countryCodeLength = prefix.Length > 0 ? Math.Min(2, digits.Length - visibleChars) : 0;
        
        var visibleStart = prefix + digits.Substring(0, countryCodeLength);
        var visibleEnd = digits.Substring(digits.Length - visibleChars);
        var maskedMiddle = new string('*', 5);

        return $"{visibleStart}{maskedMiddle}{visibleEnd}";
    }

    public bool CanViewFullData(IEnumerable<string> userRoles)
    {
        var roles = userRoles?.ToList() ?? new List<string>();

        // Admins, Managers, and SuperAdmins can view full data
        return roles.Contains(RoleNames.Administrator) ||
               roles.Contains(RoleNames.SuperAdmin) ||
               roles.Contains(RoleNames.Manager);
    }
}
