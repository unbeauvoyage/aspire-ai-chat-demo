namespace MyApi;

public class EnsureWeatherDatabaseCreatedHostedService(IServiceProvider serviceProvider, ILogger<EnsureWeatherDatabaseCreatedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync(stoppingToken);
            logger.LogInformation("Weather database ensured");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure weather database");
        }
    }
}
