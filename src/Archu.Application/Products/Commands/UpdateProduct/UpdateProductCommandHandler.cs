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

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", request.Id);
            return Result<ProductDto>.Failure("Product not found");
        }

        product.Name = request.Name;
        product.Price = request.Price;

        try
        {
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product with ID {ProductId} updated successfully", request.Id);

            // ✅ Return updated product with new RowVersion
            var updatedProductDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                RowVersion = product.RowVersion // Critical for optimistic concurrency!
            };

            return Result<ProductDto>.Success(updatedProductDto);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            // Handle concurrency exception without direct EF Core reference
            if (!await _unitOfWork.Products.ExistsAsync(request.Id, cancellationToken))
            {
                _logger.LogWarning("Product with ID {ProductId} not found during concurrency check", request.Id);
                return Result<ProductDto>.Failure("Product not found");
            }

            _logger.LogWarning("Concurrency conflict updating product with ID {ProductId}", request.Id);
            throw; // Let global exception handler format this
        }
    }
}
