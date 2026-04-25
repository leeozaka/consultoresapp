using FluentValidation;
using Homeless.Application.UseCases.Properties;

namespace Homeless.Application.Validators;

public sealed class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(x => x.Data.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Data.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Data.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.Data.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters.");

        RuleFor(x => x.Data.Bedrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bedrooms cannot be negative.");

        RuleFor(x => x.Data.Bathrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bathrooms cannot be negative.");

        RuleFor(x => x.Data.AreaSqMeters)
            .GreaterThan(0).When(x => x.Data.AreaSqMeters.HasValue)
            .WithMessage("Area must be greater than zero.");

        RuleFor(x => x.Data.Attributes)
            .Must(attrs => attrs == null || attrs.Count <= 100)
            .WithMessage("Attributes cannot exceed 100 entries.");
    }
}
