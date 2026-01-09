using TentMan.Application.Abstractions;
using TentMan.Contracts.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

public class GetTenantDocumentsQueryHandler : IRequestHandler<GetTenantDocumentsQuery, IEnumerable<TenantDocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetTenantDocumentsQueryHandler> _logger;

    public GetTenantDocumentsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<GetTenantDocumentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDocumentDto>> Handle(GetTenantDocumentsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting documents for tenant {TenantId}", request.TenantId);

        // Verify user has access to this tenant (if user context available)
        if (_currentUser.UserId != null)
        {
            var tenantByUser = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(Guid.Parse(_currentUser.UserId), cancellationToken);
            if (tenantByUser == null || tenantByUser.Id != request.TenantId)
            {
                throw new UnauthorizedAccessException("You do not have access to view documents for this tenant");
            }
        }

        var documents = await _unitOfWork.TenantDocuments.GetByTenantIdAsync(request.TenantId, cancellationToken);

        return documents.Select(d => new TenantDocumentDto
        {
            Id = d.Id,
            DocType = d.DocType,
            DocNumberMasked = d.DocNumberMasked,
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            FileId = d.FileId,
            FileName = d.File?.FileName,
            Notes = d.Notes,
            Status = d.Status,
            CreatedAtUtc = d.CreatedAtUtc
        });
    }
}
