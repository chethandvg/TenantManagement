using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Handles updating an existing product with client-side optimistic concurrency control.
/// </summary>
public class UpdateProductCommandHandler : BaseCommandHandler, IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateProductCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // ✅ Validate user authentication (throws if not authenticated)
        var userId = GetCurrentUserId("update products");

        Logger.LogInformation("User {UserId} updating product with ID: {ProductId}", userId, request.Id);

        // Fetch current entity from database
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            Logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result<ProductDto>.Failure("Product not found");
        }

        // ✅ CRITICAL: Validate RowVersion before making changes
        // This provides early detection of concurrency conflicts with a clear error message
        if (!product.RowVersion.SequenceEqual(request.RowVersion))
        {
            Logger.LogWarning(
                "Concurrency conflict detected for product {ProductId}. Client RowVersion does not match current database version",
                request.Id);

            return Result<ProductDto>.Failure(
                "The product was modified by another user. Please refresh and try again.");
        }

        // Update product properties
        product.Name = request.Name;
        product.Price = request.Price;

        try
        {
            // Use the RowVersion from the client for optimistic concurrency control
            // This ensures the client is updating the same version they retrieved
            await _unitOfWork.Products.UpdateAsync(product, request.RowVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Product with ID {ProductId} updated successfully by user {UserId}", request.Id, userId);

            // Return updated product with new RowVersion
            var updatedProductDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                RowVersion = product.RowVersion
            };

            return Result<ProductDto>.Success(updatedProductDto);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            // This catch block handles race conditions where the product was modified
            // between our RowVersion check and SaveChangesAsync call
            if (!await _unitOfWork.Products.ExistsAsync(request.Id, cancellationToken))
            {
                Logger.LogWarning("Product with ID {ProductId} was deleted during update operation", request.Id);
                return Result<ProductDto>.Failure("Product not found");
            }

            Logger.LogWarning(
                "Race condition detected: Product {ProductId} was modified between validation and save",
                request.Id);

            return Result<ProductDto>.Failure(
                "The product was modified by another user. Please refresh and try again.");
        }
    }
}
