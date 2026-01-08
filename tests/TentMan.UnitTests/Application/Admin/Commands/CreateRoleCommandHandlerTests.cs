using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Admin.Commands.CreateRole;
using TentMan.Contracts.Admin;
using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class CreateRoleCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRoleDoesNotExist_ShouldCreateRole(
        string roleName,
        string description,
        Guid adminId,
        Guid roleId,
        byte[] rowVersion,
        DateTime createdAtUtc)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var fixture = new CommandHandlerTestFixture<CreateRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        CancellationToken capturedLookupToken = default;
        CancellationToken capturedAddToken = default;
        CancellationToken capturedSaveToken = default;
        ApplicationRole? capturedRole = null;

        roleRepositoryMock
            .Setup(repo => repo.GetByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => capturedLookupToken = token)
            .ReturnsAsync((ApplicationRole?)null);

        roleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationRole, CancellationToken>((role, token) =>
            {
                capturedAddToken = token;
                capturedRole = role;
                role.Id = roleId;
                role.RowVersion = rowVersion;
                role.CreatedAtUtc = createdAtUtc;
            })
            .ReturnsAsync((ApplicationRole role, CancellationToken _) => role);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedSaveToken = token)
            .ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = fixture.CreateHandler();
        var command = new CreateRoleCommand(roleName, description);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
        result.Name.Should().Be(roleName);
        result.NormalizedName.Should().Be(roleName.ToUpperInvariant());
        result.Description.Should().Be(description);
        result.CreatedAtUtc.Should().Be(createdAtUtc);
        result.RowVersion.Should().BeSameAs(rowVersion);

        capturedRole.Should().NotBeNull();
        capturedRole!.Name.Should().Be(roleName);
        capturedRole.NormalizedName.Should().Be(roleName.ToUpperInvariant());
        capturedRole.Description.Should().Be(description);

        roleRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(cancellationTokenSource.Token), Times.Once());

        capturedLookupToken.Should().Be(cancellationTokenSource.Token);
        capturedAddToken.Should().Be(cancellationTokenSource.Token);
        capturedSaveToken.Should().Be(cancellationTokenSource.Token);

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["UserId"] = adminId.ToString(),
                ["RoleName"] = roleName,
                ["{OriginalFormat}"] = "User {UserId} creating role: {RoleName}"
            },
            Times.Once());
        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["{OriginalFormat}"] = "Role created with ID: {RoleId}"
            },
            Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRoleExists_ShouldThrowInvalidOperationException(
        string roleName,
        string description,
        Guid adminId)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var fixture = new CommandHandlerTestFixture<CreateRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        roleRepositoryMock
            .Setup(repo => repo.GetByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = Guid.NewGuid(), Name = roleName });

        var handler = fixture.CreateHandler();
        var command = new CreateRoleCommand(roleName, description);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Role '{roleName}' already exists");

        fixture.VerifyWarningLogged("Role {RoleName} already exists");
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        roleRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
