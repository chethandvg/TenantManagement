using TentMan.Contracts.Products;
using MediatR;

namespace TentMan.Application.Products.Queries.GetProductById;

/// <summary>
/// Query to retrieve a specific product by ID.
/// </summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
