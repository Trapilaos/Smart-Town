using System.Text.Json;
using System.Text.Json.Serialization;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public static class ParkingSpaceSeed
    {
        public static async Task SeedParkingSpaces(DataContext context, string contentRootPath)
        {
            var seeds = await LoadAsync(contentRootPath);
            if (seeds.Count == 0) return;

            if (!await context.ParkingSpaces.AnyAsync())
            {
                await context.ParkingSpaces.AddRangeAsync(seeds);
                await context.SaveChangesAsync();
                return;
            }

            // Backfill: rows created before coordinates existed sit at (0, 0). Copy the seed's location details.
            var unlocated = await context.ParkingSpaces
                .Where(p => p.Latitude == 0 && p.Longitude == 0)
                .ToListAsync();

            foreach (var space in unlocated)
            {
                var seed = seeds.FirstOrDefault(s => s.Id == space.Id);
                if (seed == null) continue;

                space.Address ??= seed.Address;
                space.Type = seed.Type;
                space.Latitude = seed.Latitude;
                space.Longitude = seed.Longitude;
                space.HourlyRate = seed.HourlyRate;
            }

            if (unlocated.Count > 0) await context.SaveChangesAsync();
        }

        private static async Task<List<ParkingSpace>> LoadAsync(string contentRootPath)
        {
            var json = await File.ReadAllTextAsync(Path.Combine(contentRootPath, "Data", "parking-spaces.json"));
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Deserialize<List<ParkingSpace>>(json, options) ?? new List<ParkingSpace>();
        }
    }
}
