using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    /// <summary>
    /// Deletes parking reservations that have ended. Listing spaces never writes, so expired rows are removed
    /// here instead. Runs at startup and then every 15 minutes. A failed pass is logged and tried again later.
    /// </summary>
    public class ReservationCleanupService : BackgroundService
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(IServiceScopeFactory scopes, ILogger<ReservationCleanupService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PurgeExpiredAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return; // host is shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Removing expired parking reservations failed; it will be retried");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        /// <summary>Removes every reservation that ended before now. Returns how many were removed.</summary>
        public async Task<int> PurgeExpiredAsync(CancellationToken ct = default)
        {
            using var scope = _scopes.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var now = DateTime.UtcNow;
            // Loaded into memory because SQLite cannot translate DateTime arithmetic on columns.
            var expired = (await context.Reservations.ToListAsync(ct))
                .Where(r => r.ReservationTime.AddMinutes(r.Duration) <= now)
                .ToList();

            if (expired.Count == 0) return 0;

            context.Reservations.RemoveRange(expired);
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Removed {Count} expired parking reservations", expired.Count);
            return expired.Count;
        }
    }
}
