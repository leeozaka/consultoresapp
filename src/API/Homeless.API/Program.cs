using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Homeless.API.Extensions;
using Homeless.API.Middleware;
using Homeless.Application;
using Homeless.Infrastructure;
using Homeless.Infrastructure.Metrics;
using Homeless.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext());

    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddMeter(MetricsService.MeterName);
            metrics.AddPrometheusExporter();
        });

    var app = builder.Build();

    var isNonProduction = app.Environment.IsDevelopment()
                         || app.Environment.EnvironmentName is "Docker" or "Staging";

    if (isNonProduction)
    {
        await app.ApplyMigrationsAsync();
        await DataSeeder.SeedAsync(app.Services, builder.Configuration);
    }

    // Forwarded headers MUST be first so Request.Host, Scheme and RemoteIp
    // reflect the original client values when behind the K8s ingress or the
    // Angular SSR Node process.
    app.UseForwardedHeaders();

    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseCors();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    app.UseMiddleware<TenantResolutionMiddleware>();

    if (isNonProduction)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapPrometheusScrapingEndpoint("/metrics");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Expose Program to integration tests (WebApplicationFactory<Program>)
public partial class Program { }
