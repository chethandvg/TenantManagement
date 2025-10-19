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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        
        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result.Failure("Product not found");
        }

        await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product with ID {ProductId} deleted successfully", request.Id);
        return Result.Success();
    }
}
