using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.DeleteProduct;

/// <summary>
/// Handles soft deletion of a product.
/// </summary>
public class DeleteProductCommandHandler : BaseCommandHandler, IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteProductCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // ✅ Validate user authentication (throws if not authenticated)
        var userId = GetCurrentUserId("delete products");

        Logger.LogInformation("User {UserId} deleting product with ID: {ProductId}", userId, request.Id);

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        
        if (product is null)
        {
            Logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result.Failure("Product not found");
        }

        await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Product with ID {ProductId} deleted successfully by user {UserId}", request.Id, userId);
        return Result.Success();
    }
}
