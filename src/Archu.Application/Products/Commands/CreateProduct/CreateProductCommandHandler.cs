using Archu.Application.Abstractions;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles the creation of a new product.
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product: {ProductName}", request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created with ID: {ProductId}", createdProduct.Id);

        return new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };
    }
}
