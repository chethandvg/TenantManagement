using System.Security.Claims;
using Archu.Domain.Constants;
using Archu.Ui.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Archu.Ui.Tests.Services;

public sealed class UiAuthorizationServiceTests
{
    private class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;

        public TestAuthenticationStateProvider(ClaimsPrincipal user)
        {
            _user = user;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_user));
        }
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsAuthenticatedUser()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task HasPermissionAsync_WithValidPermission_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasPermissionAsync(PermissionNames.Products.Create);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasPermissionAsync(PermissionNames.Products.Delete);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasPermissionAsync(PermissionNames.Products.Create);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnyPermissionAsync_WithOneMatchingPermission_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasAnyPermissionAsync(
            PermissionNames.Products.Create,
            PermissionNames.Products.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasAllPermissionsAsync_WithAllRequiredPermissions_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create),
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasAllPermissionsAsync(
            PermissionNames.Products.Create,
            PermissionNames.Products.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasAllPermissionsAsync_WithMissingPermission_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasAllPermissionsAsync(
            PermissionNames.Products.Create,
            PermissionNames.Products.Read);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasRoleAsync_WithValidRole_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasRoleAsync(RoleNames.User);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasRoleAsync_WithoutRole_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasRoleAsync(RoleNames.Administrator);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnyRoleAsync_WithOneMatchingRole_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasAnyRoleAsync(RoleNames.Administrator, RoleNames.User);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasAllRolesAsync_WithAllRequiredRoles_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User),
            new Claim(ClaimTypes.Role, RoleNames.Manager)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authStateProvider = new TestAuthenticationStateProvider(principal);
        var service = new UiAuthorizationService(authStateProvider);

        // Act
        var result = await service.HasAllRolesAsync(RoleNames.User, RoleNames.Manager);

        // Assert
        Assert.True(result);
    }
}
