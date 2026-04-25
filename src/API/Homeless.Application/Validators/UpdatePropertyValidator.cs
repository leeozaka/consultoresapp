using FluentValidation;
using Homeless.Application.UseCases.Properties;

namespace Homeless.Application.Validators;

public sealed class UpdatePropertyValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.Data.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Data.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Data.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Data.State)
            .NotEmpty().WithMessage("State is required.");

        RuleFor(x => x.Data.PropertyType)
            .IsInEnum().WithMessage("Property type is invalid.");

        RuleFor(x => x.Data.ListingType)
            .IsInEnum().WithMessage("Listing type is invalid.");

        RuleFor(x => x.Data.Bedrooms)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Data.Bathrooms)
            .GreaterThanOrEqualTo(0);
    }
}
