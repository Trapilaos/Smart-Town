using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Seeds
{
    public static class WasteBinSeed
    {
        private const int Capacity = 240;

        private static readonly string[] Streets =
        {
            "Leoforos Vouliagmenis", "Kanari", "Ethnikis Antistaseos", "Kyprou", "Lazaraki", "Asklipiou",
            "Metaxa", "Agiou Konstantinou", "Irodou Attikou", "Kalamou", "Dimokratias", "Ippokratous",
        };

        private static readonly WasteType[] Types =
        {
            WasteType.General, WasteType.Recycling, WasteType.Glass, WasteType.Organic, WasteType.Paper,
        };

        /// <summary>
        /// Seeds about 24 bins inside central Glyfada (37.855–37.875 lat, 23.745–23.765 lng), only when the table is empty.
        /// </summary>
        public static async Task SeedWasteBinsAsync(DataContext context)
        {
            if (await context.WasteBins.AnyAsync()) return;

            await context.WasteBins.AddRangeAsync(Generate());
            await context.SaveChangesAsync();
        }

        public static List<WasteBin> Generate()
        {
            var bins = new List<WasteBin>();
            var index = 0;
            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 6; col++)
                {
                    // Deterministic, varied fill levels between 0% and 100%.
                    var percent = (index * 53) % 101;
                    bins.Add(new WasteBin
                    {
                        Location = $"{Streets[index % Streets.Length]} {index + 1}, Glyfada",
                        Latitude = 37.8600 + row * 0.0050,
                        Longitude = 23.7500 + col * 0.0030,
                        Type = Types[index % Types.Length],
                        Capacity = Capacity,
                        CurrentFillLevel = Capacity * percent / 100,
                        LastEmptied = DateTime.UtcNow.AddDays(-(index % 5)),
                        IsActive = true,
                    });
                    index++;
                }
            }
            return bins;
        }
    }
}
