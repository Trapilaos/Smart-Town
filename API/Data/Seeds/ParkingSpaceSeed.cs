using API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Data
{
    public static class ParkingSpaceSeed
    {
        public static async Task SeedParkingSpaces(DataContext context, string contentRootPath)
        {
            if (!await context.ParkingSpaces.AnyAsync())
            {
                var parkingData = await File.ReadAllTextAsync(Path.Combine(contentRootPath, "Data", "parking-spaces.json"));
                var parkingSpaces = JsonSerializer.Deserialize<List<ParkingSpace>>(parkingData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parkingSpaces != null)
                {
                    await context.ParkingSpaces.AddRangeAsync(parkingSpaces);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
