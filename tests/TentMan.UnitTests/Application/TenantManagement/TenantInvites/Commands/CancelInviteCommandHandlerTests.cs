using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.TenantManagement.TenantInvites.Commands.CancelInvite;
using TentMan.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement.TenantInvites.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "TenantInvites")]
public class CancelInviteCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantInviteRepository> _mockInviteRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<CancelInviteCommandHandler>> _mockLogger;
    private readonly CancelInviteCommandHandler _handler;

    public CancelInviteCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockInviteRepository = new Mock<ITenantInviteRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<CancelInviteCommandHandler>>();

        _mockCurrentUser.Setup(x => x.UserId).Returns("test-user-id");
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new CancelInviteCommandHandler(
            _mockUnitOfWork.Object,
            _mockInviteRepository.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenInviteExists_CancelsInviteSuccessfully()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var invite = new TenantInvite
        {
            Id = inviteId,
            OrgId = orgId,
            TenantId = tenantId,
            InviteToken = "test-token",
            Phone = "+1234567890",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        _mockInviteRepository
            .Setup(x => x.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CancelInviteCommand(orgId, inviteId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        invite.IsDeleted.Should().BeTrue();
        _mockInviteRepository.Verify(
            x => x.UpdateAsync(invite, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInviteNotFound_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        _mockInviteRepository
            .Setup(x => x.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvite?)null);

        var command = new CancelInviteCommand(orgId, inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenInviteBelongsToDifferentOrg_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var differentOrgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        var invite = new TenantInvite
        {
            Id = inviteId,
            OrgId = differentOrgId, // Different organization
            TenantId = Guid.NewGuid(),
            InviteToken = "test-token",
            Phone = "+1234567890",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false
        };

        _mockInviteRepository
            .Setup(x => x.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var command = new CancelInviteCommand(orgId, inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("does not belong to organization");
    }

    [Fact]
    public async Task Handle_WhenInviteAlreadyUsed_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        var invite = new TenantInvite
        {
            Id = inviteId,
            OrgId = orgId,
            TenantId = Guid.NewGuid(),
            InviteToken = "test-token",
            Phone = "+1234567890",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = true, // Already used
            UsedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        _mockInviteRepository
            .Setup(x => x.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var command = new CancelInviteCommand(orgId, inviteId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already been used");
    }
}
