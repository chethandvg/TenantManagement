using Archu.Contracts.Common;
using Archu.Contracts.Products;
using MediatR;

namespace Archu.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to retrieve all active products with pagination.
/// </summary>
public record GetProductsQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<ProductDto>>;
