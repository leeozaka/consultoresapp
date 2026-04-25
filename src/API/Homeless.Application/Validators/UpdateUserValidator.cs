using FluentValidation;
using Homeless.Application.UseCases.Users;

namespace Homeless.Application.Validators;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.")
            .When(x => x.Email is not null);
    }
}
