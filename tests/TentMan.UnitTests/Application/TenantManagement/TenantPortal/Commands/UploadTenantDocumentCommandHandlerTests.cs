using TentMan.Application.Abstractions;
using TentMan.Application.TenantManagement.TenantPortal.Commands.UploadTenantDocument;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Tenants;
using TentMan.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement.TenantPortal.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "TenantDocuments")]
public class UploadTenantDocumentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<UploadTenantDocumentCommandHandler>> _mockLogger;
    private readonly UploadTenantDocumentCommandHandler _handler;

    public UploadTenantDocumentCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<UploadTenantDocumentCommandHandler>>();

        _handler = new UploadTenantDocumentCommandHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_FileSizeExceedsLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenant = new Tenant { Id = tenantId, OrgId = orgId };

        _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());
        _mockUnitOfWork.Setup(u => u.Tenants.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        using var stream = new MemoryStream();
        var request = new TenantDocumentUploadRequest { DocType = DocumentType.IDProof };
        var command = new UploadTenantDocumentCommand(
            tenantId,
            stream,
            "test.pdf",
            "application/pdf",
            11 * 1024 * 1024, // 11MB - exceeds limit
            request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidContentType_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenant = new Tenant { Id = tenantId, OrgId = orgId };

        _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());
        _mockUnitOfWork.Setup(u => u.Tenants.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        using var stream = new MemoryStream();
        var request = new TenantDocumentUploadRequest { DocType = DocumentType.IDProof };
        var command = new UploadTenantDocumentCommand(
            tenantId,
            stream,
            "test.exe",
            "application/x-msdownload", // Invalid content type
            1024,
            request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TenantNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _mockUnitOfWork.Setup(u => u.Tenants.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        using var stream = new MemoryStream();
        var request = new TenantDocumentUploadRequest { DocType = DocumentType.IDProof };
        var command = new UploadTenantDocumentCommand(
            tenantId,
            stream,
            "test.pdf",
            "application/pdf",
            1024,
            request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserNotLinkedToTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();

        var tenant = new Tenant { Id = tenantId, OrgId = orgId };
        var differentTenant = new Tenant { Id = differentTenantId, OrgId = orgId };

        _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());
        _mockUnitOfWork.Setup(u => u.Tenants.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentTenant); // Different tenant linked to user

        using var stream = new MemoryStream();
        var request = new TenantDocumentUploadRequest { DocType = DocumentType.IDProof };
        var command = new UploadTenantDocumentCommand(
            tenantId,
            stream,
            "test.pdf",
            "application/pdf",
            1024,
            request);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
