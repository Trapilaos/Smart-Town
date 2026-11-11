using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    /// <summary>
    /// Keeps the (simulated) traffic data fresh. Data is only regenerated when the stored data has run out,
    /// never blindly on startup.
    /// </summary>
    public class TrafficDataBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TrafficDataBackgroundService> _logger;

        public TrafficDataBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<TrafficDataBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval);

            do
            {
                try
                {
                    await RefreshIfStaleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A failed refresh must never take the host down; try again on the next tick.
                    _logger.LogError(ex, "Failed to refresh traffic data");
                }
            }
            while (await WaitForNextTickAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken token)
        {
            try
            {
                return await timer.WaitForNextTickAsync(token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task RefreshIfStaleAsync(CancellationToken token)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var hasFutureData = await context.TrafficData.AnyAsync(t => t.Timestamp > DateTime.UtcNow, token);
            if (hasFutureData) return;

            context.TrafficData.RemoveRange(await context.TrafficData.ToListAsync(token));
            await context.TrafficData.AddRangeAsync(TrafficDataGenerator.GenerateTrafficData(), token);
            await context.SaveChangesAsync(token);

            _logger.LogInformation("Traffic data regenerated");
        }
    }
}
