using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Contracts.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.UpdateProduct;

/// <summary>
/// Handles updating an existing product.
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

        // Fetch current entity from database - this gives us the current RowVersion
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result<ProductDto>.Failure("Product not found");
        }

        // Store the current RowVersion BEFORE making changes
        var currentRowVersion = product.RowVersion;

        // Update product properties
        product.Name = request.Name;
        product.Price = request.Price;

        try
        {
            // Use the RowVersion from the client (optimistic concurrency)
            // This ensures we're comparing against the state the client last saw
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
            // Handle concurrency exception - this means someone else modified the record
            // between our GetByIdAsync and SaveChangesAsync calls
            if (!await _unitOfWork.Products.ExistsAsync(request.Id, cancellationToken))
            {
                _logger.LogWarning("Product with ID {ProductId} not found during concurrency check", request.Id);
                return Result<ProductDto>.Failure("Product not found");
            }

            _logger.LogWarning("Concurrency conflict updating product with ID {ProductId}. The product was modified by another user.", request.Id);
            return Result<ProductDto>.Failure("The product was modified by another user. Please refresh and try again.");
        }
    }
}
