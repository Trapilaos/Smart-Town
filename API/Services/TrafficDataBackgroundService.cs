using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class TrafficDataBackgroundService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;

        public TrafficDataBackgroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteAsync, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Dispose();
            return Task.CompletedTask;
        }

        private async void ExecuteAsync(object state)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Clear existing data
            var existingData = await context.TrafficData.ToListAsync();
            context.TrafficData.RemoveRange(existingData);
            await context.SaveChangesAsync();

            // Generate new data
            var newData = TrafficDataGenerator.GenerateTrafficData();
            await context.TrafficData.AddRangeAsync(newData);
            await context.SaveChangesAsync();
        }
    }
}