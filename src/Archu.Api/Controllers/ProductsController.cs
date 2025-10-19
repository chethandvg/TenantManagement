using Archu.Application.Abstractions;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Archu.Api.Controllers;

/// <summary>
/// Exposes CRUD endpoints that orchestrate the product catalog workflow.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public partial class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    public ProductsController(IProductRepository repository, ILogger<ProductsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all active products from the catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of product data transfer objects.</returns>
    /// <response code="200">Returns the list of products.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        LogRetrievingProducts();
        
        var products = await _repository.GetAllAsync(cancellationToken);
        
        var productDtos = products.Select(product => new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        });

        LogProductsRetrieved(productDtos.Count());
        return Ok(productDtos);
    }

    /// <summary>
    /// Retrieves a specific product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product data transfer object.</returns>
    /// <response code="200">Returns the requested product.</response>
    /// <response code="404">If the product is not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        LogRetrievingProduct(id);
        
        var product = await _repository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            LogProductNotFound(id);
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        };

        return Ok(productDto);
    }

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created product data transfer object.</returns>
    /// <response code="201">Returns the newly created product.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        LogCreatingProduct(request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        var createdProduct = await _repository.AddAsync(product, cancellationToken);

        var dto = new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };

        LogProductCreated(dto.Id);
        return CreatedAtAction(nameof(GetProduct), new { id = dto.Id, version = "1.0" }, dto);
    }

    /// <summary>
    /// Updates an existing product in the catalog.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="request">The product update request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <response code="204">If the product was successfully updated.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="404">If the product is not found.</response>
    /// <response code="409">If there is a concurrency conflict.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (id != request.Id)
        {
            return BadRequest(new { message = "The route ID does not match the request payload ID." });
        }

        LogUpdatingProduct(id);

        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            LogProductNotFound(id);
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        product.Name = request.Name;
        product.Price = request.Price;

        try
        {
            // Note: Concurrency handling would need to be improved in the repository
            // For now, this will throw DbUpdateConcurrencyException which is caught by global handler
            await _repository.UpdateAsync(product, cancellationToken);
            LogProductUpdated(id);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _repository.ExistsAsync(id, cancellationToken))
            {
                LogProductNotFound(id);
                return NotFound(new { message = $"Product with ID {id} was not found." });
            }

            // Global exception handler will format this properly
            throw;
        }
    }

    /// <summary>
    /// Soft deletes a product from the catalog.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <response code="204">If the product was successfully deleted.</response>
    /// <response code="404">If the product is not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        LogDeletingProduct(id);

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        
        if (!deleted)
        {
            LogProductNotFound(id);
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        LogProductDeleted(id);
        return NoContent();
    }

    #region Logging

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving all products")]
    private partial void LogRetrievingProducts();

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} products")]
    private partial void LogProductsRetrieved(int count);

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
