using Archu.Contracts.Products;
using MediatR;

namespace Archu.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product in the catalog.
/// </summary>
public record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<ProductDto>;
