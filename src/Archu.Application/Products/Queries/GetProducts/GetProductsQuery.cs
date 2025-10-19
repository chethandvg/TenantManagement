using Archu.Contracts.Products;
using MediatR;

namespace Archu.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to retrieve all active products.
/// </summary>
public record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;
