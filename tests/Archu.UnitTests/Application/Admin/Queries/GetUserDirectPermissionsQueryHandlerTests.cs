using System.Collections.Generic;
using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Queries.GetUserDirectPermissions;
using Archu.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class GetUserDirectPermissionsQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnUserPermissions_WhenUserExists(Guid userId, string permissionName)
    {
        // Arrange
        var user = new ApplicationUser { Id = userId };
        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        userPermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission.NormalizedName });

        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.UserPermissions).Returns(userPermissionRepositoryMock.Object);
        unitOfWorkMock.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetUserDirectPermissionsQueryHandler>>();

        var handler = new GetUserDirectPermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetUserDirectPermissionsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.Permissions.Should().ContainSingle(dto => dto.Id == permission.Id);
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

        var loggerMock = new Mock<ILogger<GetUserDirectPermissionsQueryHandler>>();

        var handler = new GetUserDirectPermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetUserDirectPermissionsQuery(userId);

        // Act
        var action = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
