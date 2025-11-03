using System.Security.Claims;
using Archu.ApiClient.Authentication.Extensions;
using Archu.Contracts.Authentication.Constants;
using Xunit;

namespace Archu.ApiClient.Tests.Authentication.Extensions;

public sealed class ClaimsPrincipalAuthorizationExtensionsTests
{
    [Fact]
    public void HasPermission_WithValidPermission_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create),
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasPermission(PermissionNames.Products.Create);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasPermission(PermissionNames.Products.Delete);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasPermission(PermissionNames.Products.Create);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAnyPermission_WithOneMatchingPermission_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAnyPermission(
            PermissionNames.Products.Create,
            PermissionNames.Products.Read,
            PermissionNames.Products.Delete);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_WithNoMatchingPermissions_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAnyPermission(
            PermissionNames.Products.Create,
            PermissionNames.Products.Delete);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAllPermissions_WithAllRequiredPermissions_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create),
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read),
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Update)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAllPermissions(
            PermissionNames.Products.Create,
            PermissionNames.Products.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAllPermissions_WithMissingPermission_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAllPermissions(
            PermissionNames.Products.Read,
            PermissionNames.Products.Create);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetPermissions_ReturnsAllPermissionClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create),
            new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Read),
            new Claim(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var permissions = principal.GetPermissions().ToList();

        // Assert
        Assert.Equal(2, permissions.Count);
        Assert.Contains(PermissionNames.Products.Create, permissions);
        Assert.Contains(PermissionNames.Products.Read, permissions);
    }

    [Fact]
    public void HasRole_WithValidRole_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRole(RoleNames.User);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRole_WithoutRole_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRole(RoleNames.Administrator);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRole_IsCaseInsensitive()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRole("user");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAnyRole_WithOneMatchingRole_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAnyRole(RoleNames.Administrator, RoleNames.User);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAllRoles_WithAllRequiredRoles_ReturnsTrue()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User),
            new Claim(ClaimTypes.Role, RoleNames.Manager)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAllRoles(RoleNames.User, RoleNames.Manager);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAllRoles_WithMissingRole_ReturnsFalse()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, RoleNames.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasAllRoles(RoleNames.User, RoleNames.Administrator);

        // Assert
        Assert.False(result);
    }
}
