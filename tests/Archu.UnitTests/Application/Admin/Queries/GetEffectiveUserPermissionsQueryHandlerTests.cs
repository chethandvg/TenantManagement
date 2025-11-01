using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Queries.GetEffectiveUserPermissions;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class GetEffectiveUserPermissionsQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldAggregatePermissions_FromDirectAndRoles(Guid userId)
    {
        // Arrange
        var user = new ApplicationUser { Id = userId, UserName = "user", Email = "user@example.com" };

        var roleA = new ApplicationRole { Id = Guid.NewGuid(), Name = "RoleA", NormalizedName = "ROLEA" };
        var roleB = new ApplicationRole { Id = Guid.NewGuid(), Name = "RoleB", NormalizedName = "ROLEB" };

        var directPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Direct",
            NormalizedName = "DIRECT"
        };

        var rolePermissionA = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "RoleA.Permission",
            NormalizedName = "ROLEA.PERMISSION"
        };

        var rolePermissionB = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "RoleB.Permission",
            NormalizedName = "ROLEB.PERMISSION"
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(repo => repo.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { roleA, roleB });

        var userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        userPermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { directPermission.NormalizedName });

        var rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();
        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(roleA.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { rolePermissionA.NormalizedName });

        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(roleB.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { rolePermissionB.NormalizedName });

        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { directPermission, rolePermissionA, rolePermissionB });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.UserPermissions).Returns(userPermissionRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.RolePermissions).Returns(rolePermissionRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetEffectiveUserPermissionsQueryHandler>>();

        var handler = new GetEffectiveUserPermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetEffectiveUserPermissionsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.DirectPermissions.Should().ContainSingle(dto => dto.NormalizedName == directPermission.NormalizedName);
        result.RolePermissions.Should().HaveCount(2);
        result.EffectivePermissions.Should().HaveCount(3);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenUserNotFound(Guid userId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetEffectiveUserPermissionsQueryHandler>>();

        var handler = new GetEffectiveUserPermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetEffectiveUserPermissionsQuery(userId);

        // Act
        var action = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
