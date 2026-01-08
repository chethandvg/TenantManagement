using TentMan.Application.Products.Commands.CreateProduct;
using FluentValidation;

namespace TentMan.Application.Products.Validators;

/// <summary>
/// Validator for CreateProductCommand.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero")
            .Must(price => decimal.Round(price, 2, MidpointRounding.AwayFromZero) == price)
            .WithMessage("Price must contain at most two decimal places");
    }
}
