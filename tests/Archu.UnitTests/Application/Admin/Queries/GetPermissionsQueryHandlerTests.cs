using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Queries.GetPermissions;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class GetPermissionsQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnMappedPermissions(Guid permissionId, string permissionName)
    {
        // Arrange
        var permission = new ApplicationPermission
        {
            Id = permissionId,
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant(),
            Description = "description",
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 2, 3 }
        };

        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        permissionRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var loggerMock = new Mock<ILogger<GetPermissionsQueryHandler>>();

        var handler = new GetPermissionsQueryHandler(unitOfWorkMock.Object, loggerMock.Object);
        var query = new GetPermissionsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(permissionId);
        dto.Name.Should().Be(permissionName);
        dto.Description.Should().Be(permission.Description);
    }
}
