using TentMan.Contracts.Common;
using TentMan.Contracts.Products;
using MediatR;

namespace TentMan.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to retrieve all active products with pagination.
/// </summary>
public record GetProductsQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<ProductDto>>;
