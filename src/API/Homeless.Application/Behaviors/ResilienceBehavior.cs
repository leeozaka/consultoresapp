using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace Homeless.Application.Behaviors;

public sealed class ResilienceBehavior<TRequest, TResponse>(
    ResiliencePipelineProvider<string> pipelineProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsQuery = typeof(TRequest).Name.EndsWith("Query", StringComparison.Ordinal);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Queries are idempotent — apply the full pipeline (timeout + retry + circuit breaker).
        // Commands are NOT idempotent by default — apply only timeout + circuit breaker (no retry)
        // to prevent duplicate side effects when a command succeeds but the response is lost.
        var pipelineKey = IsQuery ? "queries" : "commands";
        var pipeline = pipelineProvider.GetPipeline(pipelineKey);

        return await pipeline.ExecuteAsync(
            async ct => await next().ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }
}
