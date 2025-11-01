using System.Security.Claims;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Common;
using Archu.Domain.Constants;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Infrastructure.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenRolePermissionsConfigured_EmitsPermissionClaimsFromDatabase()
    {
        // Arrange
        var user = CreateUserWithRole(RoleNames.Manager);
        var permissionEntities = new[]
        {
            new ApplicationPermission
            {
                Name = PermissionNames.Products.Read,
                NormalizedName = PermissionNames.Products.Read.ToUpperInvariant()
            }
        };

        var (service, getCapturedClaims) = CreateService(
            user,
            rolePermissionNames: new[] { PermissionNames.Products.Read.ToUpperInvariant() },
            userPermissionNames: Array.Empty<string>(),
            permissionEntities);

        // Act
        var result = await service.LoginAsync(user.Email, "password", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var permissionClaims = getCapturedClaims()
            .Where(claim => claim.Type == CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToList();

        permissionClaims.Should().ContainSingle().Which.Should().Be(PermissionNames.Products.Read);
    }

    [Fact]
    public async Task LoginAsync_WhenUserHasDirectPermissions_EmitsUserPermissionClaims()
    {
        // Arrange
        var user = CreateUserWithoutRoles();
        var permissionEntities = new[]
        {
            new ApplicationPermission
            {
                Name = PermissionNames.Users.Manage,
                NormalizedName = PermissionNames.Users.Manage.ToUpperInvariant()
            }
        };

        var (service, getCapturedClaims) = CreateService(
            user,
            rolePermissionNames: Array.Empty<string>(),
            userPermissionNames: new[] { PermissionNames.Users.Manage.ToUpperInvariant() },
            permissionEntities);

        // Act
        var result = await service.LoginAsync(user.Email, "password", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var permissionClaims = getCapturedClaims()
            .Where(claim => claim.Type == CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToList();

        permissionClaims.Should().ContainSingle().Which.Should().Be(PermissionNames.Users.Manage);
    }

    [Fact]
    public async Task LoginAsync_WhenPermissionsComeFromRolesAndUser_DeduplicatesEffectiveClaims()
    {
        // Arrange
        var user = CreateUserWithRole(RoleNames.Manager);
        var normalizedRolePermissions = new[]
        {
            PermissionNames.Products.Read.ToUpperInvariant(),
            PermissionNames.Products.Update.ToUpperInvariant()
        };
        var normalizedUserPermissions = new[]
        {
            PermissionNames.Products.Read.ToUpperInvariant(),
            PermissionNames.Users.Read.ToUpperInvariant()
        };

        var permissionEntities = new[]
        {
            new ApplicationPermission
            {
                Name = PermissionNames.Products.Read,
                NormalizedName = PermissionNames.Products.Read.ToUpperInvariant()
            },
            new ApplicationPermission
            {
                Name = PermissionNames.Products.Update,
                NormalizedName = PermissionNames.Products.Update.ToUpperInvariant()
            },
            new ApplicationPermission
            {
                Name = PermissionNames.Users.Read,
                NormalizedName = PermissionNames.Users.Read.ToUpperInvariant()
            }
        };

        var (service, getCapturedClaims) = CreateService(
            user,
            normalizedRolePermissions,
            normalizedUserPermissions,
            permissionEntities);

        // Act
        var result = await service.LoginAsync(user.Email, "password", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var permissionClaims = getCapturedClaims()
            .Where(claim => claim.Type == CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToList();

        permissionClaims.Should().BeEquivalentTo(new[]
        {
            PermissionNames.Products.Read,
            PermissionNames.Products.Update,
            PermissionNames.Users.Read
        });
    }

    private static ApplicationUser CreateUserWithRole(string roleName)
    {
        var roleId = Guid.NewGuid();
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            NormalizedEmail = "user@example.com".ToUpperInvariant(),
            UserName = "user",
            PasswordHash = "hashed-password",
            EmailConfirmed = true,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            UserRoles =
            {
                new UserRole
                {
                    RoleId = roleId,
                    Role = new ApplicationRole
                    {
                        Id = roleId,
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    }
                }
            }
        };
    }

    private static ApplicationUser CreateUserWithoutRoles()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            NormalizedEmail = "user@example.com".ToUpperInvariant(),
            UserName = "user",
            PasswordHash = "hashed-password",
            EmailConfirmed = true,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            UserRoles = new List<UserRole>()
        };
    }

    private static (AuthenticationService Service, Func<IReadOnlyList<Claim>> GetCapturedClaims) CreateService(
        ApplicationUser user,
        IEnumerable<string> rolePermissionNames,
        IEnumerable<string> userPermissionNames,
        IEnumerable<ApplicationPermission> permissionEntities)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();
        var userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var loggerMock = new Mock<ILogger<AuthenticationService>>();

        userRepositoryMock
            .Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepositoryMock
            .Setup(repo => repo.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        unitOfWorkMock.SetupGet(u => u.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.RolePermissions).Returns(rolePermissionRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.UserPermissions).Returns(userPermissionRepositoryMock.Object);
        unitOfWorkMock.SetupGet(u => u.Permissions).Returns(permissionRepositoryMock.Object);
        unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissionNames.ToArray());

        userPermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissionNames.ToArray());

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissionEntities.ToArray());

        passwordHasherMock
            .Setup(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        timeProviderMock
            .SetupGet(provider => provider.UtcNow)
            .Returns(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var capturedClaims = new List<Claim>();
        jwtTokenServiceMock
            .Setup(service => service.GenerateAccessToken(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<Claim>?>()))
            .Returns("access-token")
            .Callback((string _, string _, string _, IEnumerable<string> _, IEnumerable<Claim>? claims) =>
            {
                capturedClaims = claims?.ToList() ?? new List<Claim>();
            });

        jwtTokenServiceMock
            .Setup(service => service.GenerateRefreshToken())
            .Returns("refresh-token");

        var service = new AuthenticationService(
            unitOfWorkMock.Object,
            passwordHasherMock.Object,
            jwtTokenServiceMock.Object,
            timeProviderMock.Object,
            loggerMock.Object);

        return (service, () => capturedClaims);
    }
}
