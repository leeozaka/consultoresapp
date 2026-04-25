using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Onboarding;

public sealed record DismissSignupCommand(Guid TenantId, string DismissToken) : IRequest<Result>;
