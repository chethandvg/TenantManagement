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
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerIdGuid))
        {
            _logger.LogError("Cannot create product: User ID not found or invalid");
            throw new InvalidOperationException("User must be authenticated to create products");
        }

        _logger.LogInformation("User {UserId} creating product: {ProductName}", userId, request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            OwnerId = ownerIdGuid
        };

        var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product created with ID: {ProductId} by User: {UserId}",
            createdProduct.Id,
            userId);

        return new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };
    }
}
