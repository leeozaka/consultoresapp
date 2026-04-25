using FluentValidation;
using Homeless.Application.UseCases.Tenants;

namespace Homeless.Application.Validators;

public sealed class UpdateTenantSettingsValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    public UpdateTenantSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Agency name is required.")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.");
    }
}
