using Microsoft.EntityFrameworkCore;
using Homeless.Infrastructure.Persistence.Context;

namespace Homeless.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StartupMigration");
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!dbContext.Database.IsRelational())
        {
            logger.LogInformation("Skipping startup migrations because a relational database provider is not configured.");
            return;
        }

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully at startup.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Failed to apply database migrations at startup (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s...",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        await dbContext.Database.MigrateAsync();
    }
}
