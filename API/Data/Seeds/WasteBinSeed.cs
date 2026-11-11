using Microsoft.EntityFrameworkCore;

namespace API.Data.Seeds
{
    public static class WasteBinSeed
    {
        public static async Task SeedWasteBinsAsync(DataContext context)
        {
            // Only seed an empty table; never wipe bins that were updated through the API.
            if (await context.WasteBins.AnyAsync()) return;

            var generator = new WasteBinDataGenerator(context);
            await generator.CleanAndGenerateDataAsync(7); // Generate data for 7 days
        }
    }
}
