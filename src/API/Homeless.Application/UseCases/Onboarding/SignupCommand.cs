using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Onboarding;

/// <summary>
/// Anonymous command that creates a tenant + user and initiates Stripe checkout in one atomic operation.
/// Called from the self-service signup wizard.
/// </summary>
public sealed record SignupCommand(
    // Agency info
    string AgencyName,
    string Slug,
    string ContactEmail,
    string? ContactPhone,

    // Plan selection
    Guid PlanId,

    // Owner account
    string FirstName,
    string LastName,
    string Email,
    string Password,

    // Stripe redirect URLs
    string SuccessUrl,
    string CancelUrl
) : IRequest<Result<SignupResponse>>;
