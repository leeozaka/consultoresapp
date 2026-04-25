using FluentValidation;
using Homeless.Application.UseCases.Tenants;

namespace Homeless.Application.Validators;

public sealed class UpdateCurrentTenantBrandingValidator : AbstractValidator<UpdateCurrentTenantBrandingCommand>
{
    public UpdateCurrentTenantBrandingValidator()
    {
        RuleFor(x => x.Request.PrimaryColor)
            .NotEmpty()
            .Matches("^#([0-9A-Fa-f]{6})$")
            .WithMessage("Primary color must be a #RRGGBB hex value.");

        RuleFor(x => x.Request.SecondaryColor)
            .NotEmpty()
            .Matches("^#([0-9A-Fa-f]{6})$")
            .WithMessage("Secondary color must be a #RRGGBB hex value.");

        RuleFor(x => x.Request.AgencyDisplayName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
