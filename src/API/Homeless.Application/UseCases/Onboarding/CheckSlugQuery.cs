using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Onboarding;

/// <summary>
/// Anonymous query for real-time slug availability check in the signup wizard.
/// </summary>
public sealed record CheckSlugQuery(string Slug) : IRequest<Result<SlugAvailabilityResponse>>;

public sealed record SlugAvailabilityResponse(string Slug, bool Available);
