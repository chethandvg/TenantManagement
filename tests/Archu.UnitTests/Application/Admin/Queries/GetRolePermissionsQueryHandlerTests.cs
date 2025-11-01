using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Queries.GetRolePermissions;
using Archu.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class GetRolePermissionsQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnRolePermissions_WhenRoleExists(Guid roleId, string roleName, string permissionName)
    {
        // Arrange
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        };

        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();
        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission.NormalizedName });

        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.RolePermissions).Returns(rolePermissionRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetRolePermissionsQueryHandler>>();

        var handler = new GetRolePermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.RoleId.Should().Be(roleId);
        result.Permissions.Should().ContainSingle(dto => dto.Id == permission.Id);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenRoleNotFound(Guid roleId)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole?)null);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetRolePermissionsQueryHandler>>();

        var handler = new GetRolePermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var action = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
