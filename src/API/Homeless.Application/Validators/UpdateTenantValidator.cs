using FluentValidation;
using Homeless.Application.UseCases.Tenants;

namespace Homeless.Application.Validators;

public sealed class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex =
        new(@"^[a-z0-9]+(-[a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex DomainRegex =
        new(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9\-]*[a-z0-9])?)*\.[a-z]{2,}$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public UpdateTenantValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Agency name is required.")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(63).WithMessage("Slug cannot exceed 63 characters.")
            .Matches(SlugRegex).WithMessage("Slug must be lowercase alphanumeric with hyphens only.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.");

        RuleFor(x => x.CustomDomain)
            .MaximumLength(253).WithMessage("Domain cannot exceed 253 characters.")
            .Matches(DomainRegex).WithMessage("Custom domain must be a valid domain name.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomDomain));

        RuleFor(x => x.NextBillingDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Next billing date must be in the future.")
            .When(x => x.NextBillingDate.HasValue);
    }
}
