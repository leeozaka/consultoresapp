using FluentAssertions;
using Homeless.Application.DTOs;
using Homeless.Infrastructure.Messaging;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public class InMemoryImageProcessedStreamTests
{
    private readonly InMemoryImageProcessedStream _stream = new();

    private static ImageProcessedEvent MakeEvent(Guid propertyId, string key = "raw/img.jpg") =>
        new(propertyId, key, "https://cdn/full.webp", "https://cdn/thumb.webp", "https://cdn/medium.webp", true);

    [Fact]
    public async Task PublishAsync_ShouldDeliverToMatchingPropertyReader()
    {
        var propertyId = Guid.NewGuid();
        var evt = MakeEvent(propertyId);

        await _stream.PublishAsync(evt);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<ImageProcessedEvent>();

        await foreach (var e in _stream.ReadAllAsync(propertyId, cts.Token))
        {
            received.Add(e);
            break; // we only published one event
        }

        received.Should().ContainSingle();
        received[0].PropertyId.Should().Be(propertyId);
        received[0].ImageKey.Should().Be("raw/img.jpg");
        received[0].IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAllAsync_ShouldNotReceiveEventsFromOtherProperties()
    {
        var prop1 = Guid.NewGuid();
        var prop2 = Guid.NewGuid();

        await _stream.PublishAsync(MakeEvent(prop1, "key-for-prop1"));

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var received = new List<ImageProcessedEvent>();

        try
        {
            await foreach (var e in _stream.ReadAllAsync(prop2, cts.Token))
            {
                received.Add(e);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected — no events arrive for prop2
        }

        received.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverMultipleEventsInOrder()
    {
        var propertyId = Guid.NewGuid();

        await _stream.PublishAsync(MakeEvent(propertyId, "key-1"));
        await _stream.PublishAsync(MakeEvent(propertyId, "key-2"));
        await _stream.PublishAsync(MakeEvent(propertyId, "key-3"));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<ImageProcessedEvent>();

        await foreach (var e in _stream.ReadAllAsync(propertyId, cts.Token))
        {
            received.Add(e);
            if (received.Count == 3) break;
        }

        received.Should().HaveCount(3);
        received.Select(e => e.ImageKey).Should().ContainInOrder("key-1", "key-2", "key-3");
    }
}
