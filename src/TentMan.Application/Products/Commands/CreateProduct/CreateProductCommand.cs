using TentMan.Contracts.Products;
using MediatR;

namespace TentMan.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product in the catalog.
/// </summary>
public record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<ProductDto>;
