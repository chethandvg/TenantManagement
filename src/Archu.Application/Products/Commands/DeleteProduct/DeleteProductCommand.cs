using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Products.Commands.DeleteProduct;

/// <summary>
/// Command to soft delete a product.
/// </summary>
public record DeleteProductCommand(Guid Id) : IRequest<Result>;
