using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Homeless.Application.Behaviors;
using Homeless.Application.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Homeless.UnitTests.Application;

[Collection("SerilogTests")]
public class LoggingBehaviorTests : IDisposable
{
    private readonly ILogger<LoggingBehavior<TraceableTestCommand, Result<string>>> _logger;
    private readonly InMemorySink _sink;
    private readonly LoggingBehavior<TraceableTestCommand, Result<string>> _behavior;

    public LoggingBehaviorTests()
    {
        _sink = new InMemorySink();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Sink(_sink)
            .CreateLogger();

        _logger = Substitute.For<ILogger<LoggingBehavior<TraceableTestCommand, Result<string>>>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _behavior = new LoggingBehavior<TraceableTestCommand, Result<string>>(_logger);
    }

    public void Dispose()
    {
        Log.CloseAndFlush();
    }

    [Fact]
    public async Task Handle_WithTraceableRequest_ShouldReturnResponseSuccessfully()
    {
        var command = new TraceableTestCommand("credit", "ACC-001", "TXN-TRACE-001");

        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result.Success("ok"));

        var result = await _behavior.Handle(command, next, CancellationToken.None);

        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithTraceableRequest_ShouldEnrichLogsWithReferenceId()
    {
        var command = new TraceableTestCommand("credit", "ACC-001", "TXN-ENRICH-001");

        RequestHandlerDelegate<Result<string>> next = () =>
        {
            Log.Information("Inner handler executed");
            return Task.FromResult(Result.Success("ok"));
        };

        await _behavior.Handle(command, next, CancellationToken.None);

        var enrichedEvent = _sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text == "Inner handler executed");

        enrichedEvent.Should().NotBeNull();
        enrichedEvent!.Properties.Should().ContainKey("ReferenceId");
        enrichedEvent.Properties["ReferenceId"].ToString().Should().Contain("TXN-ENRICH-001");
    }

    [Fact]
    public async Task Handle_WithTraceableRequest_ShouldEnrichLogsWithAccountIdAndOperation()
    {
        var command = new TraceableTestCommand("debit", "ACC-TRACE-002", "TXN-TRACE-002");

        RequestHandlerDelegate<Result<string>> next = () =>
        {
            Log.Information("Processing debit");
            return Task.FromResult(Result.Success("ok"));
        };

        await _behavior.Handle(command, next, CancellationToken.None);

        var logEvent = _sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text == "Processing debit");

        logEvent.Should().NotBeNull();
        logEvent!.Properties.Should().ContainKey("AccountId");
        logEvent.Properties["AccountId"].ToString().Should().Contain("ACC-TRACE-002");

        logEvent.Properties.Should().ContainKey("Operation");
        logEvent.Properties["Operation"].ToString().Should().Contain("debit");
    }

    [Fact]
    public async Task Handle_WithTraceableRequest_OnError_ShouldStillHaveTraceProperties()
    {
        var command = new TraceableTestCommand("debit", "ACC-ERR", "TXN-ERR-001");

        RequestHandlerDelegate<Result<string>> next = () =>
        {
            Log.Error("Something failed inside handler");
            throw new InvalidOperationException("test error");
        };

        var act = async () => await _behavior.Handle(command, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var errorEvent = _sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text == "Something failed inside handler");

        errorEvent.Should().NotBeNull();
        errorEvent!.Properties.Should().ContainKey("ReferenceId");
        errorEvent.Properties["ReferenceId"].ToString().Should().Contain("TXN-ERR-001");
    }

    [Fact]
    public async Task Handle_WithNonTraceableRequest_ShouldNotPushTraceProperties()
    {
        var nonTraceableLogger = Substitute.For<ILogger<LoggingBehavior<NonTraceableCommand, string>>>();
        nonTraceableLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var behavior = new LoggingBehavior<NonTraceableCommand, string>(nonTraceableLogger);
        var command = new NonTraceableCommand("test");

        RequestHandlerDelegate<string> next = () =>
        {
            Log.Information("Non-traceable handler executed");
            return Task.FromResult("done");
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);
        result.Should().Be("done");

        var logEvent = _sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text == "Non-traceable handler executed");

        logEvent.Should().NotBeNull();
        logEvent!.Properties.Should().NotContainKey("ReferenceId");
        logEvent.Properties.Should().NotContainKey("AccountId");
        logEvent.Properties.Should().NotContainKey("Operation");
    }

    [Fact]
    public void TraceableTestCommand_ShouldImplementITraceable()
    {
        var command = new TraceableTestCommand("credit", "ACC-001", "TXN-001");

        command.Should().BeAssignableTo<ITraceable>();

        var traceable = (ITraceable)command;
        traceable.ReferenceId.Should().Be("TXN-001");
        traceable.AccountId.Should().Be("ACC-001");
        traceable.Operation.Should().Be("credit");
    }

    public sealed record TraceableTestCommand(
        string Operation, string AccountId, string ReferenceId)
        : IRequest<Result<string>>, ITraceable;

    public sealed record NonTraceableCommand(string Data) : IRequest<string>;
}

public sealed class InMemorySink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];
    private readonly object _lock = new();

    public IReadOnlyList<LogEvent> LogEvents
    {
        get
        {
            lock (_lock)
                return _events.ToList();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_lock)
            _events.Add(logEvent);
    }
}
