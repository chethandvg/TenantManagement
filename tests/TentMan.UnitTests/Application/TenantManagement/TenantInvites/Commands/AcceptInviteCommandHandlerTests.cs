using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using TentMan.Application.TenantManagement.TenantInvites.Commands.AcceptInvite;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement.TenantInvites.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "TenantInvites")]
public class AcceptInviteCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITenantInviteRepository> _mockInviteRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<AcceptInviteCommandHandler>> _mockLogger;
    private readonly AcceptInviteCommandHandler _handler;

    public AcceptInviteCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockInviteRepository = new Mock<ITenantInviteRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockLogger = new Mock<ILogger<AcceptInviteCommandHandler>>();

        // Setup UnitOfWork to return mocked repositories
        _mockUnitOfWork.Setup(x => x.Tenants).Returns(_mockTenantRepository.Object);
        _mockUnitOfWork.Setup(x => x.Roles).Returns(_mockRoleRepository.Object);
        _mockUnitOfWork.Setup(x => x.UserRoles).Returns(_mockUserRoleRepository.Object);

        _handler = new AcceptInviteCommandHandler(
            _mockUnitOfWork.Object,
            _mockInviteRepository.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object,
            _mockCurrentUser.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidInvite_CreatesUserAndAssignsTenantRole()
    {
        // Arrange
        var inviteToken = "valid-token-12345";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = tenantId,
            InviteToken = inviteToken,
            Phone = "+1234567890",
            Email = "tenant@example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var tenant = new Tenant
        {
            Id = tenantId,
            OrgId = invite.OrgId,
            FullName = "John Doe",
            Phone = invite.Phone,
            Email = invite.Email,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var tenantRole = new ApplicationRole
        {
            Id = roleId,
            Name = ApplicationRoles.Tenant,
            NormalizedName = ApplicationRoles.Tenant.ToUpperInvariant()
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserRepository.Setup(x => x.GetByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");

        ApplicationUser? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((user, _) =>
            {
                user.Id = userId; // Simulate ID assignment
                capturedUser = user;
            })
            .ReturnsAsync((ApplicationUser user, CancellationToken _) => user);

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockRoleRepository.Setup(x => x.GetByNameAsync(ApplicationRoles.Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantRole);

        _mockJwtTokenService.Setup(x => x.GenerateAccessToken(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");

        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _mockJwtTokenService.Setup(x => x.GetAccessTokenExpiration())
            .Returns(TimeSpan.FromMinutes(60));

        _mockJwtTokenService.Setup(x => x.GetRefreshTokenExpiryUtc())
            .Returns(DateTime.UtcNow.AddDays(7));

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.UserName.Should().Be("johndoe");
        result.User.Email.Should().Be("tenant@example.com");
        result.User.Roles.Should().Contain(ApplicationRoles.Tenant);

        // Verify invite was marked as used
        _mockInviteRepository.Verify(x => x.UpdateAsync(
            It.Is<TenantInvite>(i => i.IsUsed && i.AcceptedByUserId == userId),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify tenant was linked to user
        _mockTenantRepository.Verify(x => x.UpdateAsync(
            It.Is<Tenant>(t => t.LinkedUserId == userId),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Tenant role was assigned
        _mockUserRoleRepository.Verify(x => x.AddAsync(
            It.Is<UserRole>(ur => ur.UserId == userId && ur.RoleId == roleId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify changes were saved
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "invalid-token";

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvite?)null);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyUsedInvite_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "used-token";

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InviteToken = inviteToken,
            Phone = "+1234567890",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = true, // Already used
            UsedAtUtc = DateTime.UtcNow.AddDays(-1),
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already been used");

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredInvite_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "expired-token";

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InviteToken = inviteToken,
            Phone = "+1234567890",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("expired");

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "valid-token";

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InviteToken = inviteToken,
            Phone = "+1234567890",
            Email = "expected@example.com", // Specific email required
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "different@example.com", // Different email
            "SecurePassword123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("does not match");

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingEmailAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "valid-token";

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InviteToken = inviteToken,
            Phone = "+1234567890",
            Email = "tenant@example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existinguser",
            Email = "tenant@example.com"
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockUserRepository.Setup(x => x.GetByEmailAsync("tenant@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Unable to create account");

        // Verify no new user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var inviteToken = "valid-token";

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InviteToken = inviteToken,
            Phone = "+1234567890",
            Email = "tenant@example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "johndoe",
            Email = "other@example.com"
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserRepository.Setup(x => x.GetByUserNameAsync("johndoe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var command = new AcceptInviteCommand(
            inviteToken,
            "johndoe",
            "tenant@example.com",
            "SecurePassword123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already taken");

        // Verify no new user was created
        _mockUserRepository.Verify(x => x.AddAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LinksUserToTenantEntity()
    {
        // Arrange
        var inviteToken = "valid-token";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            TenantId = tenantId,
            InviteToken = inviteToken,
            Phone = "+1234567890",
            Email = "tenant@example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var tenant = new Tenant
        {
            Id = tenantId,
            OrgId = invite.OrgId,
            FullName = "John Doe",
            Phone = invite.Phone,
            LinkedUserId = null, // Not linked yet
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        _mockInviteRepository.Setup(x => x.GetByTokenAsync(inviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserRepository.Setup(x => x.GetByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-password");

        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((user, _) => user.Id = userId)
            .ReturnsAsync((ApplicationUser user, CancellationToken _) => user);

        _mockTenantRepository.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockRoleRepository.Setup(x => x.GetByNameAsync(ApplicationRoles.Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = Guid.NewGuid(), Name = ApplicationRoles.Tenant });

        _mockJwtTokenService.Setup(x => x.GenerateAccessToken(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");

        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _mockJwtTokenService.Setup(x => x.GetAccessTokenExpiration()).Returns(TimeSpan.FromMinutes(60));
        _mockJwtTokenService.Setup(x => x.GetRefreshTokenExpiryUtc()).Returns(DateTime.UtcNow.AddDays(7));

        var command = new AcceptInviteCommand(inviteToken, "johndoe", "tenant@example.com", "SecurePassword123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTenantRepository.Verify(x => x.UpdateAsync(
            It.Is<Tenant>(t => t.Id == tenantId && t.LinkedUserId == userId),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
