using TentMan.Application.Abstractions;
using TentMan.Contracts.Common;
using TentMan.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Products.Queries.GetProducts;

/// <summary>
/// Handles retrieving paginated active products.
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
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

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving products - Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);

        var (products, totalCount) = await _repository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            RowVersion = p.RowVersion
        }).ToList();

        _logger.LogInformation("Retrieved {Count} products out of {TotalCount} total", productDtos.Count, totalCount);

        return PagedResult<ProductDto>.Create(productDtos, request.PageNumber, request.PageSize, totalCount);
    }
}
