using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Infrastructure.Messaging;

public sealed class SiteBuildBackgroundService(
    ISiteBuildChannel channel,
    IServiceScopeFactory scopeFactory,
    ISiteBuildStatusStream statusStream,
    ILogger<SiteBuildBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Site build background service started");

        await foreach (var message in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var provisioningService = scope.ServiceProvider.GetRequiredService<ISiteProvisioningService>();

                await PublishAsync(message.TenantId, "queued", "queued", "Site build queued", stoppingToken);

                await PublishAsync(message.TenantId, "in_progress", "dns_validation", "Validating DNS", stoppingToken);
                await provisioningService.ValidateDnsAsync(message, stoppingToken).ConfigureAwait(false);

                await PublishAsync(message.TenantId, "in_progress", "routing_sync", "Synchronizing site routing", stoppingToken);
                await provisioningService.ProvisionCaddyAsync(message, stoppingToken).ConfigureAwait(false);

                await PublishAsync(message.TenantId, "in_progress", "content_seed", "Provisioning default content", stoppingToken);
                await provisioningService.ProvisionDefaultContentAsync(message, stoppingToken).ConfigureAwait(false);

                await PublishAsync(message.TenantId, "completed", "done", "Site build completed", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Site build failed for tenant {TenantId}", message.TenantId);
                await PublishAsync(
                    message.TenantId,
                    "failed",
                    "error",
                    "Site build failed. Check logs for details.",
                    stoppingToken);
            }
        }

        logger.LogInformation("Site build background service stopped");
    }

    private async Task PublishAsync(
        Guid tenantId,
        string status,
        string step,
        string message,
        CancellationToken cancellationToken)
    {
        await statusStream.PublishAsync(
            new SiteBuildStatusEventResponse(
                tenantId,
                status,
                step,
                message,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }
}
