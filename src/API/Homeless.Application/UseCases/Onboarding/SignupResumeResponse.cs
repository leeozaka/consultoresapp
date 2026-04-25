namespace Homeless.Application.UseCases.Onboarding;

public sealed record SignupResumeResponse(
    Guid TenantId,
    string AgencyName,
    string Slug,
    string ContactEmail,
    string? ContactPhone,
    Guid PlanId,
    string OwnerEmail,
    string OwnerFirstName,
    string OwnerLastName,
    string OnboardingState
);
