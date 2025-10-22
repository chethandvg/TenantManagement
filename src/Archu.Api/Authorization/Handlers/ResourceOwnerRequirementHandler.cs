using Archu.Api.Authorization.Requirements;
using Archu.Application.Abstractions;
using Archu.Domain.Abstractions.Identity;
using Archu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Archu.Api.Authorization.Handlers;

/// <summary>
/// Handler for ResourceOwnerRequirement.
/// Checks if the authenticated user owns the requested resource.
/// Administrators bypass ownership checks.
/// </summary>
public sealed class ResourceOwnerRequirementHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResourceOwnerRequirementHandler> _logger;

    public ResourceOwnerRequirementHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<ResourceOwnerRequirementHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return;
        }

        // ✅ Admins bypass ownership checks
        if (context.User.IsInRole(RoleNames.Administrator) || context.User.IsInRole(RoleNames.SuperAdmin))
        {
            _logger.LogDebug("User {UserId} is admin, bypassing ownership check", userId);
            context.Succeed(requirement);
            return;
        }

        // If no specific resource ID provided, check will be done at repository level
        if (!requirement.ResourceId.HasValue)
        {
            _logger.LogDebug("No resource ID specified, ownership check deferred to repository");
            context.Succeed(requirement);
            return;
        }

        // Get the resource type from the HTTP context route data
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HTTP context not available");
            return;
        }

        var controller = httpContext.GetRouteValue("controller")?.ToString()?.ToLowerInvariant();
        
        bool isOwner = controller switch
        {
            "products" => await CheckProductOwnershipAsync(requirement.ResourceId.Value, userId),
            // Add more resource types here as needed
            _ => false
        };

        if (isOwner)
        {
            _logger.LogDebug(
                "User {UserId} is owner of {Controller} resource {ResourceId}",
                userId,
                controller,
                requirement.ResourceId);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} is NOT owner of {Controller} resource {ResourceId}",
                userId,
                controller,
                requirement.ResourceId);
        }
    }

    private async Task<bool> CheckProductOwnershipAsync(Guid resourceId, Guid userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var productRepository = scope.ServiceProvider.GetService<IProductRepository>();

        if (productRepository == null)
        {
            _logger.LogError("Product repository not available");
            return false;
        }

        var product = await productRepository.GetByIdAsync(resourceId);
        
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", resourceId);
            return false;
        }

        // Check if product implements IHasOwner
        if (product is IHasOwner ownedResource)
        {
            return ownedResource.IsOwnedBy(userId);
        }

        // If product doesn't implement IHasOwner, check CreatedBy from audit
        // This is a fallback for entities that haven't been updated yet
        var createdByString = product.CreatedBy;
        if (!string.IsNullOrEmpty(createdByString) && Guid.TryParse(createdByString, out var creatorId))
        {
            return creatorId == userId;
        }

        _logger.LogWarning("Product {ProductId} has no owner information", resourceId);
        return false;
    }
}
