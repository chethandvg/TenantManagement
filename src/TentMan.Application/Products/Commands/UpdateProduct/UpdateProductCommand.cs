using TentMan.Application.Common;
using TentMan.Contracts.Products;
using MediatR;

namespace TentMan.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    byte[] RowVersion
) : IRequest<Result<ProductDto>>;
