using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class SiteBuildChannel : ISiteBuildChannel
{
    private readonly Channel<SiteBuildMessage> _channel;

    public SiteBuildChannel(IOptions<SiteBuildOptions> options)
    {
        var capacity = options.Value.ChannelCapacity;
        _channel = Channel.CreateBounded<SiteBuildMessage>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
    }

    public ValueTask WriteAsync(SiteBuildMessage message, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<SiteBuildMessage> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class SiteBuildOptions
{
    public const string SectionName = "SiteBuild";
    public int ChannelCapacity { get; set; } = 100;
}
