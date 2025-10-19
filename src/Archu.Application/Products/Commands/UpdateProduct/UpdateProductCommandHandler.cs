using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Handles updating an existing product with client-side optimistic concurrency control.
/// </summary>
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", request.Id);

        // Fetch current entity from database
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result<ProductDto>.Failure("Product not found");
        }

        // ✅ CRITICAL: Validate RowVersion before making changes
        // This provides early detection of concurrency conflicts with a clear error message
        if (!product.RowVersion.SequenceEqual(request.RowVersion))
        {
            _logger.LogWarning(
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

            _logger.LogInformation("Product with ID {ProductId} updated successfully", request.Id);

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
                _logger.LogWarning("Product with ID {ProductId} was deleted during update operation", request.Id);
                return Result<ProductDto>.Failure("Product not found");
            }

            _logger.LogWarning(
                "Race condition detected: Product {ProductId} was modified between validation and save",
                request.Id);

            return Result<ProductDto>.Failure(
                "The product was modified by another user. Please refresh and try again.");
        }
    }
}
