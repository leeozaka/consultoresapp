using Homeless.Application.Interfaces;
using Homeless.Domain.Interfaces.Repositories.Read;
using Homeless.Domain.Interfaces.Repositories.Write;

namespace Homeless.Worker.Maintenance;

public sealed class TenantMaintenanceWorker(
    IServiceProvider serviceProvider,
    ILogger<TenantMaintenanceWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TenantMaintenanceWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMaintenanceTasksAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during maintenance tasks");
            }

            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunMaintenanceTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var tenantReadRepository = scope.ServiceProvider.GetRequiredService<ITenantReadRepository>();
        var tenantWriteRepository = scope.ServiceProvider.GetRequiredService<ITenantWriteRepository>();
        var propertyReadRepository = scope.ServiceProvider.GetRequiredService<IPropertyReadRepository>();
        var propertyWriteRepository = scope.ServiceProvider.GetRequiredService<IPropertyWriteRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await DeactivateStalePropertiesAsync(
            propertyReadRepository, propertyWriteRepository, unitOfWork, cancellationToken)
            .ConfigureAwait(false);

        await SuspendOverdueTenantsAsync(
            tenantReadRepository, tenantWriteRepository, unitOfWork, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task DeactivateStalePropertiesAsync(
        IPropertyReadRepository propertyReadRepository,
        IPropertyWriteRepository propertyWriteRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-1);
        var staleProperties = await propertyReadRepository
            .GetStaleActivePropertiesAsync(cutoffDate, cancellationToken)
            .ConfigureAwait(false);

        if (staleProperties.Count == 0) return;

        logger.LogInformation("Found {Count} stale properties to deactivate", staleProperties.Count);

        foreach (var property in staleProperties)
        {
            property.Deactivate();
            await propertyWriteRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Successfully deactivated {Count} stale properties", staleProperties.Count);
    }

    private async Task SuspendOverdueTenantsAsync(
        ITenantReadRepository tenantReadRepository,
        ITenantWriteRepository tenantWriteRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        const int graceDays = 7;
        var overdueTenants = await tenantReadRepository
            .GetOverdueTenantsAsync(graceDays, cancellationToken)
            .ConfigureAwait(false);

        if (overdueTenants.Count == 0) return;

        logger.LogInformation("Found {Count} overdue tenants to suspend", overdueTenants.Count);

        foreach (var tenant in overdueTenants)
        {
            tenant.Suspend("Payment overdue — auto-suspended after 7-day grace period.");
            await tenantWriteRepository.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Successfully suspended {Count} overdue tenants", overdueTenants.Count);
    }
}
