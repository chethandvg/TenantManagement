using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.DeleteProduct;

/// <summary>
/// Handles soft deletion of a product.
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository repository,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);

        var deleted = await _repository.DeleteAsync(request.Id, cancellationToken);
        
        if (!deleted)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result.Failure("Product not found");
        }

        _logger.LogInformation("Product with ID {ProductId} deleted successfully", request.Id);
        return Result.Success();
    }
}
