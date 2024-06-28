

namespace API.Data.Seeds
{
    public static class WasteBinSeed
    {
        public static async Task SeedWasteBinsAsync(DataContext context)
        {
            var generator = new WasteBinDataGenerator(context);
            await generator.CleanAndGenerateDataAsync(7); // Generate data for 7 days
        }
    }
}
