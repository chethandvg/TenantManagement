namespace TentMan.Application.Abstractions.Security;

/// <summary>
/// Service for masking sensitive data before returning it to unauthorized users.
/// </summary>
public interface IDataMaskingService
{
    /// <summary>
    /// Masks a document number or sensitive identifier.
    /// Example: "ABCD1234567890" -> "****7890"
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <param name="visibleChars">Number of characters to show at the end (default: 4).</param>
    /// <returns>The masked value or empty string if value is null/empty.</returns>
    string MaskDocumentNumber(string? value, int visibleChars = 4);

    /// <summary>
    /// Masks an email address.
    /// Example: "user@example.com" -> "u***@example.com"
    /// </summary>
    string MaskEmail(string? email);

    /// <summary>
    /// Masks a phone number.
    /// Example: "+1234567890" -> "+1*****7890"
    /// </summary>
    string MaskPhoneNumber(string? phone, int visibleChars = 4);

    /// <summary>
    /// Determines if a user should see full (unmasked) data based on their role.
    /// </summary>
    /// <param name="userRoles">The roles of the current user.</param>
    /// <returns>True if user is authorized to see full data.</returns>
    bool CanViewFullData(IEnumerable<string> userRoles);
}
