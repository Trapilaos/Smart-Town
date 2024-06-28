using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public static class TrafficDataSeed
    {
        public static async Task SeedTrafficData(DataContext context)
        {
            if (!await context.TrafficData.AnyAsync())
            {
                var trafficData = TrafficDataGenerator.GenerateTrafficData();
                await context.TrafficData.AddRangeAsync(trafficData);
                await context.SaveChangesAsync();
            }
        }
    }
}
