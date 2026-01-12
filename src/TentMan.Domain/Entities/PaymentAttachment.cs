using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents payment proof attachments (receipts, screenshots, documents).
/// Links FileMetadata to Payments for better organization and tracking.
/// </summary>
public class PaymentAttachment : BaseEntity
{
    public PaymentAttachment()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid PaymentId { get; set; }
    public Guid FileId { get; set; }
    
    /// <summary>
    /// Type or description of the attachment (e.g., "Receipt", "Screenshot", "Bank Statement")
    /// </summary>
    public string AttachmentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description or notes about the attachment
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Display order for multiple attachments
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public Payment Payment { get; set; } = null!;
    public FileMetadata File { get; set; } = null!;
}
