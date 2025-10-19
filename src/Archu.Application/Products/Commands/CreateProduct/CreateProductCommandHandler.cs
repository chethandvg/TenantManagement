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
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository repository,
        ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
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

        var createdProduct = await _repository.AddAsync(product, cancellationToken);

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
