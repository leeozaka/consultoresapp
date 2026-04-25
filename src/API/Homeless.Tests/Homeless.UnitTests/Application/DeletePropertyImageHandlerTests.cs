using System.Net;
using Amazon.S3;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Homeless.Application.Interfaces;
using Homeless.Application.UseCases.Properties;
using Homeless.Domain.Entities;
using Homeless.Domain.Enums;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;
using Homeless.Domain.ValueObjects;
using Xunit;

namespace Homeless.UnitTests.Application;

public class DeletePropertyImageHandlerTests
{
    private readonly IPropertyReadRepository _propertyReadRepository = Substitute.For<IPropertyReadRepository>();
    private readonly IPropertyWriteRepository _propertyWriteRepository = Substitute.For<IPropertyWriteRepository>();
    private readonly IStorageService _storageService = Substitute.For<IStorageService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestLogger<DeletePropertyImageHandler> _logger = new();
    private readonly DeletePropertyImageHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeletePropertyImageHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);

        _handler = new DeletePropertyImageHandler(
            _propertyReadRepository,
            _propertyWriteRepository,
            _storageService,
            _tenantContext,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenStorageDeleteReturnsNotFound_ShouldStillRemoveImageAndLogWarning()
    {
        var property = Property.Create(
            _tenantId,
            "Apartamento Central",
            Money.Create(50_000_000L, Currency.BRL),
            "São Paulo",
            "SP",
            PropertyType.Apartment,
            ListingType.Sale,
            2,
            2);

        property.AddImage("tenants/test/properties/prop/raw/missing.webp", "missing.webp");

        _propertyReadRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>())
            .Returns(property);

        _storageService.DeleteAsync(property.Images[0].Key, Arg.Any<CancellationToken>())
            .Returns(_ => throw new AmazonS3Exception("Object not found")
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var result = await _handler.Handle(
            new DeletePropertyImageCommand(property.Id, property.Images[0].Key),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().BeEmpty();
        await _propertyWriteRepository.Received(1).UpdateAsync(property, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _logger.Entries.Should().ContainSingle(entry =>
            entry.LogLevel == LogLevel.Warning &&
            entry.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) &&
            entry.Message.Contains("Property image", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
