using Archu.Application.Common;
using Archu.Contracts.Products;
using MediatR;

namespace Archu.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price
) : IRequest<Result<ProductDto>>;
