using FluentAssertions;
using Homeless.Application.DTOs;
using Homeless.Infrastructure.Messaging;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public class InMemoryPaymentEventStreamTests
{
    private readonly InMemoryPaymentEventStream _stream = new();

    private static PaymentEventResponse MakeEvent(
        Guid? tenantId = null,
        string status = "payment_succeeded",
        long amount = 12920,
        string detail = "Payment confirmed") =>
        new(
            tenantId ?? Guid.NewGuid(),
            "Demo Tenant",
            "Starter",
            amount,
            "brl",
            status,
            DateTime.UtcNow,
            detail);

    [Fact]
    public async Task PublishAsync_ShouldBroadcastToAllActiveReaders()
    {
        var evt = MakeEvent();
        var reader1 = _stream.ReadAllAsync().GetAsyncEnumerator();
        var reader2 = _stream.ReadAllAsync().GetAsyncEnumerator();

        try
        {
            await _stream.PublishAsync(evt);

            var moved1 = await reader1.MoveNextAsync().AsTask();
            var moved2 = await reader2.MoveNextAsync().AsTask();

            moved1.Should().BeTrue();
            moved2.Should().BeTrue();
            reader1.Current.Should().BeEquivalentTo(evt);
            reader2.Current.Should().BeEquivalentTo(evt);
        }
        finally
        {
            await reader1.DisposeAsync();
            await reader2.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReplayMostRecentEventToNewReader()
    {
        var evt = MakeEvent(status: "activated", detail: "Lead Nurturing activated");

        await _stream.PublishAsync(evt);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var reader = _stream.ReadAllAsync(cts.Token).GetAsyncEnumerator();

        var moved = await reader.MoveNextAsync().AsTask();

        moved.Should().BeTrue();
        reader.Current.Should().BeEquivalentTo(evt);
    }
}
