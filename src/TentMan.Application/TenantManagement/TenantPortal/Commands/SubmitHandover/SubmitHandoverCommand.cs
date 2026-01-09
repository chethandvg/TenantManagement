using TentMan.Contracts.TenantPortal;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Commands.SubmitHandover;

/// <summary>
/// Command to submit a completed handover checklist with signature.
/// </summary>
public record SubmitHandoverCommand(
    Guid UserId,
    SubmitHandoverRequest Request,
    byte[] SignatureImageBytes,
    string SignatureFileName,
    string SignatureContentType) : IRequest<MoveInHandoverResponse>;
