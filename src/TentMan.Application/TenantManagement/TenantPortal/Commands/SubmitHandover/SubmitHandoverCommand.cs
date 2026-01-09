using TentMan.Contracts.TenantPortal;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Commands.SubmitHandover;

/// <summary>
/// Command to submit a completed handover checklist with signature.
/// </summary>
public record SubmitHandoverCommand(
    Guid UserId,
    SubmitHandoverRequest Request,
    Stream? SignatureImageStream,
    string? SignatureFileName,
    string? SignatureContentType,
    long SignatureSize) : IRequest<MoveInHandoverResponse>;
