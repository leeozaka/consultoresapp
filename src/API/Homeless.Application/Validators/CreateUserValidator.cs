using FluentValidation;
using Homeless.Application.Authorization;
using Homeless.Application.UseCases.Users;

namespace Homeless.Application.Validators;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.Role)
            .Must(r => r is Roles.TenantAdmin or Roles.Agent)
            .When(x => x.Role is not null)
            .WithMessage($"Role must be '{Roles.TenantAdmin}' or '{Roles.Agent}'.");

        RuleFor(x => x.TenantId)
            .NotNull().When(x => x.Role is not null)
            .WithMessage("TenantId is required when a role is specified.");

        RuleFor(x => x.Role)
            .NotNull().When(x => x.TenantId.HasValue)
            .WithMessage("Role is required when a TenantId is specified.");
    }
}
