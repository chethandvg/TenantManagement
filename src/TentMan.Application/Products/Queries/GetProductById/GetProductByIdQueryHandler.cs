using TentMan.Application.Abstractions;
using TentMan.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Products.Queries.GetProductById;

/// <summary>
/// Handles retrieving a specific product by ID.
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IProductRepository repository,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", request.Id);

        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return null;
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        };
    }
}
