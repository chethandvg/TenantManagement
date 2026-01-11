using TentMan.Shared.Constants.Authorization;
using TentMan.Application.Products.Commands.CreateProduct;
using TentMan.Application.Products.Commands.DeleteProduct;
using TentMan.Application.Products.Commands.UpdateProduct;
using TentMan.Application.Products.Queries.GetProductById;
using TentMan.Application.Products.Queries.GetProducts;
using TentMan.Contracts.Common;
using TentMan.Contracts.Products;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers;

/// <summary>
/// Exposes CRUD endpoints that orchestrate the product catalog workflow using CQRS pattern.
/// Demonstrates role-based and permission-based authorization.
/// All endpoints require authentication and specific roles or permissions.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize] // Require authentication for all endpoints
public partial class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all active products from the catalog with pagination.
    /// </summary>
    /// <remarks>
    /// Accessible by all authenticated users (User, Manager, Admin roles).
    /// Demonstrates basic read access for all user roles.
    /// </remarks>
    /// <param name="pageNumber">The page number (1-based, defaults to 1).</param>
    /// <param name="pageSize">The page size (defaults to 10, max 100).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response containing the paginated list of products.</returns>
    /// <response code="200">Returns the paginated list of products.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have the required role or permission.</response>
    [HttpGet]
    [Authorize(Policy = PolicyNames.Products.View)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Enforce page size bounds
        if (pageSize < 1)
            pageSize = 1;
        if (pageSize > 100)
            pageSize = 100;
        if (pageNumber < 1)
            pageNumber = 1;

        LogRetrievingProducts();

        var pagedResult = await _mediator.Send(new GetProductsQuery(pageNumber, pageSize), cancellationToken);

        LogProductsRetrieved(pagedResult.Items.Count(), pagedResult.TotalCount);
        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult, "Products retrieved successfully"));
    }

    /// <summary>
    /// Retrieves a specific product by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Accessible by all authenticated users (User, Manager, Admin roles).
    /// Demonstrates basic read access for individual product details.
    /// </remarks>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response containing the product.</returns>
    /// <response code="200">Returns the requested product.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have the required role or permission.</response>
    /// <response code="404">If the product is not found.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.Products.View)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        LogRetrievingProduct(id);

        var product = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);

        if (product is null)
        {
            LogProductNotFound(id);
            return NotFound(ApiResponse<ProductDto>.Fail($"Product with ID {id} was not found"));
        }

        return Ok(ApiResponse<ProductDto>.Ok(product, "Product retrieved successfully"));
    }

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    /// <remarks>
    /// Restricted to Admin and Manager roles only.
    /// Regular users cannot create products.
    /// Demonstrates elevated permissions for write operations.
    /// </remarks>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response containing the created product.</returns>
    /// <response code="201">Returns the newly created product.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have Admin or Manager role.</response>
    [HttpPost]
    [Authorize(Policy = PolicyNames.Products.Create)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        LogCreatingProduct(request.Name);

        var command = new CreateProductCommand(request.Name, request.Price);
        var product = await _mediator.Send(command, cancellationToken);

        LogProductCreated(product.Id);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id, version = "1.0" },
            ApiResponse<ProductDto>.Ok(product, "Product created successfully"));
    }

    /// <summary>
    /// Updates an existing product in the catalog.
    /// </summary>
    /// <remarks>
    /// Restricted to Admin and Manager roles only.
    /// Regular users cannot update products.
    /// Demonstrates elevated permissions for modification operations.
    /// </remarks>
    /// <param name="id">The product identifier.</param>
    /// <param name="request">The product update request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response with the updated product.</returns>
    /// <response code="200">Returns the updated product with new RowVersion.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have Admin or Manager role.</response>
    /// <response code="404">If the product is not found.</response>
    /// <response code="409">If there is a concurrency conflict.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.Products.Update)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(ApiResponse<ProductDto>.Fail("The route ID does not match the request payload ID"));
        }

        LogUpdatingProduct(id);

        var command = new UpdateProductCommand(request.Id, request.Name, request.Price, request.RowVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            LogProductNotFound(id);
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        }

        LogProductUpdated(id);
        return Ok(ApiResponse<ProductDto>.Ok(result.Value!, "Product updated successfully"));
    }

    /// <summary>
    /// Soft deletes a product from the catalog.
    /// </summary>
    /// <remarks>
    /// Restricted to Admin role only.
    /// Only administrators can delete products.
    /// Demonstrates the highest level of access control for destructive operations.
    /// </remarks>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response.</returns>
    /// <response code="200">If the product was successfully deleted.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have Admin role.</response>
    /// <response code="404">If the product is not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyNames.Products.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        LogDeletingProduct(id);

        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            LogProductNotFound(id);
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        }

        LogProductDeleted(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Product deleted successfully"));
    }

    #region Logging

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving products with pagination")]
    private partial void LogRetrievingProducts();

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} products out of {TotalCount} total")]
    private partial void LogProductsRetrieved(int count, int totalCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving product with ID {ProductId}")]
    private partial void LogRetrievingProduct(Guid productId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Product with ID {ProductId} not found")]
    private partial void LogProductNotFound(Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating product: {ProductName}")]
    private partial void LogCreatingProduct(string productName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product created with ID {ProductId}")]
    private partial void LogProductCreated(Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating product with ID {ProductId}")]
    private partial void LogUpdatingProduct(Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product with ID {ProductId} updated successfully")]
    private partial void LogProductUpdated(Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleting product with ID {ProductId}")]
    private partial void LogDeletingProduct(Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product with ID {ProductId} deleted successfully")]
    private partial void LogProductDeleted(Guid productId);

    #endregion
}
