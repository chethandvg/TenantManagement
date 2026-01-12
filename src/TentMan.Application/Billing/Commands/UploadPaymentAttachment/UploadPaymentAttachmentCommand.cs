using MediatR;

namespace TentMan.Application.Billing.Commands.UploadPaymentAttachment;

/// <summary>
/// Command to upload a receipt/attachment to an existing payment.
/// </summary>
public class UploadPaymentAttachmentCommand : IRequest<UploadPaymentAttachmentResult>
{
    public Guid PaymentId { get; set; }
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? AttachmentType { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Result of uploading payment attachment.
/// </summary>
public class UploadPaymentAttachmentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? AttachmentId { get; set; }
    public string? FileUrl { get; set; }
}
