using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.TenantManagement.TenantInvites.Queries.GetInvitesByTenant;
using TentMan.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement.TenantInvites.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "TenantInvites")]
public class GetInvitesByTenantQueryHandlerTests
{
    private readonly Mock<ITenantInviteRepository> _mockInviteRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<GetInvitesByTenantQueryHandler>> _mockLogger;
    private readonly GetInvitesByTenantQueryHandler _handler;

    public GetInvitesByTenantQueryHandlerTests()
    {
        _mockInviteRepository = new Mock<ITenantInviteRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<GetInvitesByTenantQueryHandler>>();

        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);

        _mockCurrentUser.Setup(x => x.UserId).Returns("test-user-id");
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new GetInvitesByTenantQueryHandler(
            _mockInviteRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantHasInvites_ReturnsInvitesList()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            OrgId = orgId,
            FullName = "John Doe",
            Phone = "+1234567890"
        };

        var invites = new List<TenantInvite>
        {
            new TenantInvite
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                TenantId = tenantId,
                InviteToken = "token1",
                Phone = "+1234567890",
                Email = "john@example.com",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(4),
                IsUsed = false,
                Tenant = tenant
            },
            new TenantInvite
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                TenantId = tenantId,
                InviteToken = "token2",
                Phone = "+1234567890",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-3),
                IsUsed = false,
                Tenant = tenant
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockInviteRepository
            .Setup(x => x.GetAllByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        var query = new GetInvitesByTenantQuery(orgId, tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto =>
        {
            dto.TenantId.Should().Be(tenantId);
            dto.OrgId.Should().Be(orgId);
            dto.TenantFullName.Should().Be("John Doe");
        });
    }

    [Fact]
    public async Task Handle_WhenTenantHasNoInvites_ReturnsEmptyList()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            OrgId = orgId,
            FullName = "John Doe",
            Phone = "+1234567890"
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockInviteRepository
            .Setup(x => x.GetAllByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantInvite>());

        var query = new GetInvitesByTenantQuery(orgId, tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var query = new GetInvitesByTenantQuery(orgId, tenantId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenTenantBelongsToDifferentOrg_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var differentOrgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            OrgId = differentOrgId, // Different organization
            FullName = "John Doe",
            Phone = "+1234567890"
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var query = new GetInvitesByTenantQuery(orgId, tenantId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Contain("does not belong to organization");
    }
}
