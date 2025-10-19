using Archu.Application.Abstractions;
using Archu.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Queries.GetProducts;

/// <summary>
/// Handles retrieving all active products.
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IProductRepository repository,
        ILogger<GetProductsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all products");

        var products = await _repository.GetAllAsync(cancellationToken);

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            RowVersion = p.RowVersion
        }).ToList();

        _logger.LogInformation("Retrieved {Count} products", productDtos.Count);

        return productDtos;
    }
}
