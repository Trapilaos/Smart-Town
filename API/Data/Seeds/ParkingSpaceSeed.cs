using API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API.Data
{
    public static class ParkingSpaceSeed
    {
        public static async Task SeedParkingSpaces(DataContext context)
        {
            if (!await context.ParkingSpaces.AnyAsync())
            {
                var parkingData = await File.ReadAllTextAsync("Data/parking-spaces.json");
                var parkingSpaces = JsonSerializer.Deserialize<List<ParkingSpace>>(parkingData);

                if (parkingSpaces != null)
                {
                    await context.ParkingSpaces.AddRangeAsync(parkingSpaces);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
