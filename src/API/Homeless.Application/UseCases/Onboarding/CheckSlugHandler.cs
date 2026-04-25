using Ardalis.Result;
using MediatR;
using Homeless.Domain.Interfaces.Repositories.Read;

namespace Homeless.Application.UseCases.Onboarding;

public sealed class CheckSlugHandler(ITenantReadRepository tenantReadRepository)
    : IRequestHandler<CheckSlugQuery, Result<SlugAvailabilityResponse>>
{
    public async Task<Result<SlugAvailabilityResponse>> Handle(
        CheckSlugQuery request,
        CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLowerInvariant().Trim();

        var exists = await tenantReadRepository
            .ExistsBySlugAsync(slug, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(new SlugAvailabilityResponse(slug, Available: !exists));
    }
}
