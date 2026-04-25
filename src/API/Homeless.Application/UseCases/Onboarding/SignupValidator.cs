using FluentValidation;

namespace Homeless.Application.UseCases.Onboarding;

public sealed class SignupValidator : AbstractValidator<SignupCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex =
        new(@"^[a-z0-9]+(-[a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public SignupValidator()
    {
        RuleFor(x => x.AgencyName)
            .NotEmpty().WithMessage("Agency name is required.")
            .MaximumLength(150).WithMessage("Agency name cannot exceed 150 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(63).WithMessage("Slug cannot exceed 63 characters.")
            .Matches(SlugRegex).WithMessage("Slug must be lowercase alphanumeric with hyphens only (e.g. 'minha-imobiliaria').");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.");

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30).WithMessage("Contact phone cannot exceed 30 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan selection is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.");
    }
}
