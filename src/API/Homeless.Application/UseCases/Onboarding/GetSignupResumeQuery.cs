using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Onboarding;

public sealed record GetSignupResumeQuery(Guid TenantId) : IRequest<Result<SignupResumeResponse>>;
