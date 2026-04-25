namespace Homeless.Application.DTOs;

public sealed record SignupRequest(
    string AgencyName,
    string Slug,
    string ContactEmail,
    string? ContactPhone,
    Guid PlanId,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string SuccessUrl,
    string CancelUrl
);

public sealed record DismissSignupRequest(string DismissToken);

