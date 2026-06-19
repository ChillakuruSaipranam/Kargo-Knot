using Microsoft.EntityFrameworkCore;
using TruckTransportApi.Data;

namespace TruckTransportApi.Services;

public class DatabaseMigrationHostedService(IServiceProvider services, ILogger<DatabaseMigrationHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                logger.LogInformation("Applying database migrations...");
                await db.Database.MigrateAsync(stoppingToken);
                await DbSeeder.SeedAsync(db);
                logger.LogInformation("Database ready.");
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Database not ready, retrying in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
