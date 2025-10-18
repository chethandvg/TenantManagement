using Archu.Api.Contracts.Products;
using Archu.Domain.Entities;
using Archu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Archu.Api.Controllers;

/// <summary>
/// Exposes CRUD endpoints that orchestrate the product catalog workflow backed
/// by <see cref="ApplicationDbContext"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Wires up the database context so each action can collaborate with the
    /// persistence layer while executing the catalog workflow.
    /// </summary>
    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the current catalog so list or search screens can present
    /// up-to-date product data without tracking the entities for changes.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Select(product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                RowVersion = product.RowVersion
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    /// <summary>
    /// Loads the requested product so detail or edit forms can display the
    /// authoritative information for a single item.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                RowVersion = product.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return product;
    }

    /// <summary>
    /// Creates a product record when the onboarding workflow confirms a new
    /// catalog item, then returns the persisted shape to the caller.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, dto);
    }

    /// <summary>
    /// Applies user edits from maintenance workflows, enforcing concurrency by
    /// validating the supplied row version before committing updates.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (id != request.Id)
        {
            return BadRequest("The route id does not match the request payload.");
        }

        var product = await _context.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name;
        product.Price = request.Price;
        _context.Entry(product).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken))
            {
                return NotFound();
            }

            return Conflict("The product was updated by another process. Reload and try again.");
        }

        return NoContent();
    }

    /// <summary>
    /// Triggers the soft-delete workflow so retired items disappear from public
    /// listings while retaining audit history.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _context.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
