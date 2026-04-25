using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Homeless.Application.Interfaces;
using Homeless.Infrastructure;
using Homeless.Infrastructure.Caching;
using Homeless.Infrastructure.Concurrency;
using Homeless.Infrastructure.Messaging;
using Homeless.Infrastructure.Storage;
using Xunit;

namespace Homeless.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterRedisAndRabbitMqBackedServices_WhenDistributedInfrastructureIsConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "redis:6379,abortConnect=false",
                ["RabbitMq:Host"] = "rabbitmq",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, new TestHostEnvironment());

        services.ShouldHaveRegistration<ICacheService, RedisCacheService>();
        services.ShouldHaveRegistration<IKeyValueStore, RedisKeyValueStore>();
        services.ShouldHaveRegistration<IDistributedLockService, RedisDistributedLockService>();
        services.ShouldHaveRegistration<IEventBus, MassTransitEventBus>();
        services.ShouldHaveRegistration<ISiteProvisioningService, NoopSiteProvisioningService>();
        services.ShouldNotRegister<ICaddyAdminClient>();
        services.ShouldNotContainHostedService<CaddyReconciliationHostedService>();
    }

    [Fact]
    public void AddInfrastructure_ShouldFallbackToInMemoryServices_WhenDistributedInfrastructureIsNotConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, new TestHostEnvironment());

        services.ShouldHaveRegistration<ICacheService, InMemoryCacheService>();
        services.ShouldHaveRegistration<IKeyValueStore, InMemoryKeyValueStore>();
        services.ShouldHaveRegistration<IDistributedLockService, InMemoryDistributedLockService>();
        services.ShouldHaveRegistration<IEventBus, InMemoryEventBus>();
        services.ShouldHaveRegistration<ISiteProvisioningService, NoopSiteProvisioningService>();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Homeless.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

file static class ServiceCollectionAssertions
{
    public static void ShouldHaveRegistration<TService, TImplementation>(this IServiceCollection services)
    {
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(TService)
            && descriptor.ImplementationType == typeof(TImplementation));
    }

    public static void ShouldNotRegister<TService>(this IServiceCollection services)
    {
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(TService));
    }

    public static void ShouldNotContainHostedService<THostedService>(this IServiceCollection services)
    {
        services.Should().NotContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(THostedService));
    }
}
