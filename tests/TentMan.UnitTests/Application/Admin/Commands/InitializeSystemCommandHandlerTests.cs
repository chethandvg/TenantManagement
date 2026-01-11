using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Admin.Commands.InitializeSystem;
using TentMan.Application.Common;
using TentMan.Contracts.Enums;
using TentMan.Shared.Constants.Authorization;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class InitializeSystemCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenSystemNotInitialized_ShouldCreateRolesAndSuperAdmin(
        string userName,
        string email,
        string password,
        Guid userId,
        DateTime utcNow)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var hashedPassword = $"hashed-{password}";
        var superAdminRoleId = Guid.NewGuid();
        var createdRoles = new List<ApplicationRole>();
        var saveTokens = new List<CancellationToken>();
        var userRoleAssignments = new List<UserRole>();

        passwordHasherMock.Setup(hasher => hasher.HashPassword(password)).Returns(hashedPassword);
        timeProviderMock.SetupGet(provider => provider.UtcNow).Returns(utcNow);

        var fixture = CreateFixture(passwordHasherMock, timeProviderMock);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        using var cancellationTokenSource = new CancellationTokenSource();

        CancellationToken capturedCountToken = default;
        userRepositoryMock
            .Setup(repo => repo.GetCountAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedCountToken = token)
            .ReturnsAsync(0);

        CancellationToken capturedRetryToken = default;
        fixture.MockUnitOfWork
            .Setup(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<(bool, int, Guid, Guid?, Guid?)>>, CancellationToken>(async (operation, token) =>
            {
                capturedRetryToken = token;
                return await operation();
            });

        CancellationToken capturedBeginToken = default;
        CancellationToken capturedCommitToken = default;
        fixture.MockUnitOfWork
            .Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedBeginToken = token)
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedCommitToken = token)
            .Returns(Task.CompletedTask);

        CancellationToken capturedRollbackToken = default;
        fixture.MockUnitOfWork
            .Setup(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedRollbackToken = token)
            .Returns(Task.CompletedTask);

        CancellationToken capturedGetAllRolesToken = default;
        roleRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedGetAllRolesToken = token)
            .ReturnsAsync(Array.Empty<ApplicationRole>());

        roleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationRole, CancellationToken>((role, token) =>
            {
                token.Should().Be(cancellationTokenSource.Token);
                createdRoles.Add(role);
            })
            .ReturnsAsync((ApplicationRole role, CancellationToken _) => role);

        CancellationToken capturedGetRoleByNameToken = default;
        roleRepositoryMock
            .Setup(repo => repo.GetByNameAsync(RoleNames.SuperAdmin, It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => capturedGetRoleByNameToken = token)
            .ReturnsAsync(new ApplicationRole { Id = superAdminRoleId, Name = RoleNames.SuperAdmin });

        CancellationToken capturedUserNameExistsToken = default;
        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(userName, null, It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, CancellationToken>((_, _, token) => capturedUserNameExistsToken = token)
            .ReturnsAsync(false);

        CancellationToken capturedEmailExistsToken = default;
        userRepositoryMock
            .Setup(repo => repo.EmailExistsAsync(email, null, It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, CancellationToken>((_, _, token) => capturedEmailExistsToken = token)
            .ReturnsAsync(false);

        ApplicationUser? createdUser = null;
        userRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((user, token) =>
            {
                token.Should().Be(cancellationTokenSource.Token);
                user.Id = userId;
                createdUser = user;
            })
            .ReturnsAsync((ApplicationUser user, CancellationToken _) => user);

        CancellationToken capturedUserHasRoleToken = default;
        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, superAdminRoleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedUserHasRoleToken = token)
            .ReturnsAsync(false);

        userRoleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .Callback<UserRole, CancellationToken>((userRole, token) =>
            {
                token.Should().Be(cancellationTokenSource.Token);
                userRoleAssignments.Add(userRole);
            })
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => saveTokens.Add(token))
            .ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new InitializeSystemCommand(userName, email, password, null, null);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RolesCreated.Should().BeTrue();
        result.Value.RolesCount.Should().Be(5);
        result.Value.UserCreated.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Message.Should().Contain("System initialized successfully");

        createdRoles.Should().HaveCount(5);
        createdRoles.Select(role => role.Name).Should().BeEquivalentTo(new[]
        {
            RoleNames.Guest,
            RoleNames.User,
            RoleNames.Manager,
            RoleNames.Administrator,
            RoleNames.SuperAdmin
        });

        userRoleAssignments.Should().HaveCount(1);
        userRoleAssignments[0].UserId.Should().Be(userId);
        userRoleAssignments[0].RoleId.Should().Be(superAdminRoleId);
        userRoleAssignments[0].AssignedBy.Should().Be("System");
        userRoleAssignments[0].AssignedAtUtc.Should().Be(utcNow);

        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be(userName);
        createdUser.Email.Should().Be(email);
        createdUser.PasswordHash.Should().Be(hashedPassword);

        passwordHasherMock.Verify(hasher => hasher.HashPassword(password), Times.Once());

        capturedCountToken.Should().Be(cancellationTokenSource.Token);
        capturedRetryToken.Should().Be(cancellationTokenSource.Token);
        capturedBeginToken.Should().Be(cancellationTokenSource.Token);
        capturedCommitToken.Should().Be(cancellationTokenSource.Token);
        capturedGetAllRolesToken.Should().Be(cancellationTokenSource.Token);
        capturedGetRoleByNameToken.Should().Be(cancellationTokenSource.Token);
        capturedUserNameExistsToken.Should().Be(cancellationTokenSource.Token);
        capturedEmailExistsToken.Should().Be(cancellationTokenSource.Token);
        capturedUserHasRoleToken.Should().Be(cancellationTokenSource.Token);
        saveTokens.Should().OnlyContain(token => token == cancellationTokenSource.Token);

        fixture.MockUnitOfWork.Verify(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.BeginTransactionAsync(cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.CommitTransactionAsync(cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["{OriginalFormat}"] = "Starting system initialization..."
            },
            Times.Once());
        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["RolesCount"] = 5,
                ["Email"] = email
            },
            Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUsersAlreadyExist_ShouldReturnFailure(
        string userName,
        string email,
        string password)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var fixture = CreateFixture(passwordHasherMock, timeProviderMock);
        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new InitializeSystemCommand(userName, email, password, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("System is already initialized. Users already exist in the database.");

        fixture.VerifyWarningLogged("System initialization attempted but users already exist");
        fixture.MockUnitOfWork.Verify(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenInitializationFails_ShouldRollbackAndReturnFailure(
        string userName,
        string email,
        string password)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var exception = new InvalidOperationException("boom");

        passwordHasherMock.Setup(hasher => hasher.HashPassword(password)).Returns("hashed");

        var fixture = CreateFixture(passwordHasherMock, timeProviderMock);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        fixture.MockUnitOfWork
            .Setup(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<(bool, int, Guid, Guid?, Guid?)>>, CancellationToken>(async (operation, token) => await operation());

        fixture.MockUnitOfWork
            .Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        CancellationToken capturedRollbackToken = default;
        fixture.MockUnitOfWork
            .Setup(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedRollbackToken = token)
            .Returns(Task.CompletedTask);

        roleRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApplicationRole>());

        roleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole role, CancellationToken _) => role);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var handler = fixture.CreateHandler();
        var command = new InitializeSystemCommand(userName, email, password, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("System initialization failed: boom");

        fixture.VerifyErrorLogged("Error occurred during system initialization", exception);

        fixture.MockUnitOfWork.Verify(unit => unit.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        capturedRollbackToken.Should().Be(CancellationToken.None);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenOrganizationAndOwnerProvided_ShouldCreateAllEntities(
        string userName,
        string email,
        string password,
        string orgName,
        string ownerDisplayName,
        string ownerPhone,
        string ownerEmail,
        Guid userId,
        Guid orgId,
        Guid ownerId,
        DateTime utcNow)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var ownerRepositoryMock = new Mock<IOwnerRepository>();
        var hashedPassword = $"hashed-{password}";
        var superAdminRoleId = Guid.NewGuid();

        passwordHasherMock.Setup(hasher => hasher.HashPassword(password)).Returns(hashedPassword);
        timeProviderMock.SetupGet(provider => provider.UtcNow).Returns(utcNow);

        var fixture = CreateFixture(passwordHasherMock, timeProviderMock);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Organizations).Returns(organizationRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Owners).Returns(ownerRepositoryMock.Object);

        using var cancellationTokenSource = new CancellationTokenSource();

        userRepositoryMock
            .Setup(repo => repo.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        fixture.MockUnitOfWork
            .Setup(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<(bool, int, Guid, Guid?, Guid?)>>, CancellationToken>(async (operation, token) => await operation());

        fixture.MockUnitOfWork
            .Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        roleRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApplicationRole>());

        roleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole role, CancellationToken _) => role);

        roleRepositoryMock
            .Setup(repo => repo.GetByNameAsync(RoleNames.SuperAdmin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = superAdminRoleId, Name = RoleNames.SuperAdmin });

        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(userName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.EmailExistsAsync(email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ApplicationUser? createdUser = null;
        userRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((user, token) =>
            {
                user.Id = userId;
                createdUser = user;
            })
            .ReturnsAsync((ApplicationUser user, CancellationToken _) => user);

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, superAdminRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRoleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Organization? createdOrg = null;
        organizationRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((org, token) =>
            {
                org.Id = orgId;
                createdOrg = org;
            })
            .ReturnsAsync((Organization org, CancellationToken _) => org);

        Owner? createdOwner = null;
        ownerRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Owner>(), It.IsAny<CancellationToken>()))
            .Callback<Owner, CancellationToken>((owner, token) =>
            {
                owner.Id = ownerId;
                createdOwner = owner;
            })
            .ReturnsAsync((Owner owner, CancellationToken _) => owner);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var orgData = new OrganizationData(orgName, "Asia/Kolkata");
        var ownerData = new OwnerData(
            OwnerType.Individual,
            ownerDisplayName,
            ownerPhone,
            ownerEmail,
            null,
            null);
        var command = new InitializeSystemCommand(userName, email, password, orgData, ownerData);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RolesCreated.Should().BeTrue();
        result.Value.RolesCount.Should().Be(5);
        result.Value.UserCreated.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.OrganizationCreated.Should().BeTrue();
        result.Value.OrganizationId.Should().Be(orgId);
        result.Value.OwnerCreated.Should().BeTrue();
        result.Value.OwnerId.Should().Be(ownerId);

        createdOrg.Should().NotBeNull();
        createdOrg!.Name.Should().Be(orgName);
        createdOrg.TimeZone.Should().Be("Asia/Kolkata");
        createdOrg.CreatedBy.Should().Be("System");

        createdOwner.Should().NotBeNull();
        createdOwner!.OrgId.Should().Be(orgId);
        createdOwner.DisplayName.Should().Be(ownerDisplayName);
        createdOwner.Phone.Should().Be(ownerPhone);
        createdOwner.Email.Should().Be(ownerEmail);
        createdOwner.LinkedUserId.Should().Be(userId);
        createdOwner.CreatedBy.Should().Be("System");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenOwnerProvidedWithoutOrganization_ShouldReturnFailure(
        string userName,
        string email,
        string password,
        string ownerDisplayName,
        string ownerPhone,
        string ownerEmail)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var fixture = CreateFixture(passwordHasherMock, timeProviderMock);
        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = fixture.CreateHandler();
        var ownerData = new OwnerData(
            OwnerType.Individual,
            ownerDisplayName,
            ownerPhone,
            ownerEmail,
            null,
            null);
        var command = new InitializeSystemCommand(userName, email, password, null, ownerData);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Organization details must be provided when creating an owner.");

        fixture.VerifyWarningLogged("Owner details provided without organization details");
        fixture.MockUnitOfWork.Verify(unit => unit.ExecuteWithRetryAsync(It.IsAny<Func<Task<(bool, int, Guid, Guid?, Guid?)>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Creates a fixture configured to build the initialize system handler with password hasher and time provider dependencies.
    /// </summary>
    private static CommandHandlerTestFixture<InitializeSystemCommandHandler> CreateFixture(
        Mock<IPasswordHasher> passwordHasherMock,
        Mock<ITimeProvider> timeProviderMock)
    {
        var fixture = new CommandHandlerTestFixture<InitializeSystemCommandHandler>()
            .WithHandlerFactory((unitOfWork, _, logger) =>
                new InitializeSystemCommandHandler(unitOfWork, passwordHasherMock.Object, timeProviderMock.Object, logger));

        return fixture;
    }
}
