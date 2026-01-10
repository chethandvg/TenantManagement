using System.Net;
using System.Text.Json;
using TentMan.ApiClient.Exceptions;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Authentication;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantInvites;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.ApiClient.Tests.Services;

public sealed class TenantInvitesApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<TenantInvitesApiClient>> _mockLogger;
    private readonly TenantInvitesApiClient _apiClient;

    public TenantInvitesApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");
        
        _mockLogger = new Mock<ILogger<TenantInvitesApiClient>>();
        _apiClient = new TenantInvitesApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region ValidateInviteAsync Tests

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnValidInvite_WhenTokenIsValid()
    {
        // Arrange
        var token = "a1b2c3d4e5f67890abcdef1234567890";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = true,
            TenantFullName = "John Doe",
            Phone = "+1234567890",
            Email = "john.doe@example.com",
            ErrorMessage = null
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeTrue();
        result.Data.TenantFullName.Should().Be("John Doe");
        result.Data.Email.Should().Be("john.doe@example.com");
        result.Data.Phone.Should().Be("+1234567890");
        result.Data.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnInvalidInvite_WhenTokenIsExpired()
    {
        // Arrange
        var token = "expired-token-123456789012345678";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = false,
            TenantFullName = null,
            Phone = null,
            Email = null,
            ErrorMessage = "This invite has expired"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.ErrorMessage.Should().Be("This invite has expired");
        result.Data.TenantFullName.Should().BeNull();
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnInvalidInvite_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        var token = "used-token-1234567890123456789";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = false,
            TenantFullName = null,
            Phone = null,
            Email = null,
            ErrorMessage = "Invite has already been used"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.ErrorMessage.Should().Be("Invite has already been used");
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnInvalidInvite_WhenTokenNotFound()
    {
        // Arrange
        var token = "invalid-token-12345678901234567";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = false,
            TenantFullName = null,
            Phone = null,
            Email = null,
            ErrorMessage = "Invalid invite token"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.ErrorMessage.Should().Be("Invalid invite token");
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldThrowArgumentException_WhenTokenIsNull()
    {
        // Act & Assert - ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.ValidateInviteAsync(null!));
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldThrowArgumentException_WhenTokenIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _apiClient.ValidateInviteAsync(""));
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldEscapeTokenInUrl()
    {
        // Arrange
        var token = "token+with/special=chars";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = true,
            TenantFullName = "John Doe",
            Phone = "+1234567890",
            Email = "john.doe@example.com"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse);

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={Uri.EscapeDataString(token)}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    #endregion

    #region AcceptInviteAsync Tests

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnAuthenticationResponse_WhenInviteAccepted()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var authResponse = new AuthenticationResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken = "refresh-token-abc123",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            User = new UserInfoDto
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "johndoe",
                Email = "john.doe@example.com",
                EmailConfirmed = true,
                Roles = new[] { "Tenant" }
            }
        };

        var apiResponse = ApiResponse<AuthenticationResponse>.Ok(
            authResponse, 
            "Invite accepted and user created successfully");

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.AcceptInviteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.User.Should().NotBeNull();
        result.Data.User!.UserName.Should().Be("johndoe");
        result.Data.User.Email.Should().Be("john.doe@example.com");
        result.Data.User.Roles.Should().Contain("Tenant");
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowValidationException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "existing@example.com",
            Password = "SecurePass123!@#"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Email is already taken\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowValidationException_WhenPasswordTooWeak()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "weak"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Password does not meet complexity requirements\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowValidationException_WhenInviteExpired()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "expired-token-123456789012345678",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Invite has expired\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowValidationException_WhenInviteAlreadyUsed()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "used-token-1234567890123456789",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Invite has already been used\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.AcceptInviteAsync(null!));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldHandleServerError()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.InternalServerError, "application/json", 
                "{\"success\":false,\"message\":\"Internal server error\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ServerException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    #endregion

    #region GenerateInviteAsync Tests

    [Fact]
    public async Task GenerateInviteAsync_ShouldReturnInviteDto_WhenInviteGenerated()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GenerateInviteRequest
        {
            TenantId = tenantId,
            ExpiryDays = 7
        };

        var inviteDto = new TenantInviteDto
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            TenantId = tenantId,
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            InviteUrl = "",
            Phone = "+1234567890",
            Email = "john.doe@example.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            TenantFullName = "John Doe"
        };

        var apiResponse = ApiResponse<TenantInviteDto>.Ok(inviteDto, "Invite generated successfully");

        _mockHttp
            .When(HttpMethod.Post, $"https://api.test.com/api/v1/organizations/{orgId}/tenants/{tenantId}/invites")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GenerateInviteAsync(orgId, tenantId, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.InviteToken.Should().NotBeNullOrEmpty();
        result.Data.TenantFullName.Should().Be("John Doe");
        result.Data.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateInviteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.GenerateInviteAsync(orgId, tenantId, null!));
    }

    [Fact]
    public async Task GenerateInviteAsync_ShouldThrowValidationException_WhenTenantNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = new GenerateInviteRequest
        {
            TenantId = tenantId,
            ExpiryDays = 7
        };

        _mockHttp
            .When(HttpMethod.Post, $"https://api.test.com/api/v1/organizations/{orgId}/tenants/{tenantId}/invites")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Tenant not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.GenerateInviteAsync(orgId, tenantId, request));
    }

    #endregion

    #region GetInvitesByTenantAsync Tests

    [Fact]
    public async Task GetInvitesByTenantAsync_ShouldReturnInvitesList_WhenInvitesExist()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var invites = new List<TenantInviteDto>
        {
            new TenantInviteDto
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                TenantId = tenantId,
                InviteToken = "token1",
                Phone = "+1234567890",
                Email = "john@example.com",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(4),
                IsUsed = false,
                TenantFullName = "John Doe"
            },
            new TenantInviteDto
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                TenantId = tenantId,
                InviteToken = "token2",
                Phone = "+1234567890",
                Email = "john@example.com",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-3),
                IsUsed = false,
                TenantFullName = "John Doe"
            }
        };

        var apiResponse = ApiResponse<IEnumerable<TenantInviteDto>>.Ok(invites, "Invites retrieved successfully");

        _mockHttp
            .When(HttpMethod.Get, $"https://api.test.com/api/v1/organizations/{orgId}/tenants/{tenantId}/invites")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetInvitesByTenantAsync(orgId, tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInvitesByTenantAsync_ShouldReturnEmptyList_WhenNoInvitesExist()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var apiResponse = ApiResponse<IEnumerable<TenantInviteDto>>.Ok(
            new List<TenantInviteDto>(), 
            "Invites retrieved successfully");

        _mockHttp
            .When(HttpMethod.Get, $"https://api.test.com/api/v1/organizations/{orgId}/tenants/{tenantId}/invites")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetInvitesByTenantAsync(orgId, tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    #endregion

    #region CancelInviteAsync Tests

    [Fact]
    public async Task CancelInviteAsync_ShouldReturnSuccess_WhenInviteCanceled()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        var apiResponse = ApiResponse<bool>.Ok(true, "Invite canceled successfully");

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/organizations/{orgId}/invites/{inviteId}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.CancelInviteAsync(orgId, inviteId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelInviteAsync_ShouldThrowValidationException_WhenInviteAlreadyUsed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/organizations/{orgId}/invites/{inviteId}")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Cannot cancel an invite that has already been used\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CancelInviteAsync(orgId, inviteId));
    }

    [Fact]
    public async Task CancelInviteAsync_ShouldThrowValidationException_WhenInviteNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/organizations/{orgId}/invites/{inviteId}")
            .Respond(HttpStatusCode.BadRequest, "application/json", 
                "{\"success\":false,\"message\":\"Invite not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CancelInviteAsync(orgId, inviteId));
    }

    #endregion
}
