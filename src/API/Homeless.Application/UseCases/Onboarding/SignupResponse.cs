namespace Homeless.Application.UseCases.Onboarding;

/// <summary>
/// Returned after a successful self-service signup.
/// The client should redirect the user to <see cref="CheckoutUrl"/> to complete payment.
/// </summary>
public sealed record SignupResponse(
    Guid TenantId,
    Guid UserId,
    string CheckoutUrl,
    string DismissToken
);
