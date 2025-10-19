using Archu.Contracts.Products;
using MediatR;

namespace Archu.Application.Products.Queries.GetProductById;

/// <summary>
/// Query to retrieve a specific product by ID.
/// </summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
