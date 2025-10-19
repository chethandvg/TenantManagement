using Archu.Application.Products.Commands.UpdateProduct;
using FluentValidation;

namespace Archu.Application.Products.Validators;

/// <summary>
/// Validator for UpdateProductCommand.
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero")
            .Must(price => decimal.Round(price, 2, MidpointRounding.AwayFromZero) == price)
            .WithMessage("Price must contain at most two decimal places");
    }
}
