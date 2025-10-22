using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles the creation of a new product.
/// </summary>
public class CreateProductCommandHandler : BaseCommandHandler, IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateProductCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // ✅ IMPROVED: Use base class method for user ID extraction and validation
        var ownerIdGuid = GetCurrentUserId("create products");

        Logger.LogInformation("User {UserId} creating product: {ProductName}", ownerIdGuid, request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            OwnerId = ownerIdGuid
        };

        var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Product created with ID: {ProductId} by User: {UserId}",
            createdProduct.Id,
            ownerIdGuid);

        return new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };
    }
}
