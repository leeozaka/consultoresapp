using System.Collections.Concurrent;
using System.Threading.Channels;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class InMemorySiteBuildStatusStream : ISiteBuildStatusStream
{
    private readonly ConcurrentDictionary<Guid, Channel<SiteBuildStatusEventResponse>> _channels = new();

    public async ValueTask PublishAsync(
        SiteBuildStatusEventResponse statusEvent,
        CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(statusEvent.TenantId, _ =>
            Channel.CreateUnbounded<SiteBuildStatusEventResponse>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));

        await channel.Writer.WriteAsync(statusEvent, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<SiteBuildStatusEventResponse> ReadAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(tenantId, _ =>
            Channel.CreateUnbounded<SiteBuildStatusEventResponse>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));

        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
