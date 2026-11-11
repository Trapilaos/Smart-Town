using API.Entities;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Seeds
{
    /// <summary>
    /// Seeds the single municipality row (Glyfada). The polygon is only an approximate fallback: on first startup
    /// <see cref="BoundaryAutoFetchService"/> replaces it with the OpenStreetMap boundary when that succeeds.
    /// </summary>
    public static class MunicipalitySeed
    {
        public const string ApproximateSource = "Approximate seed (Glyfada)";

        // Approximate outline, GeoJSON order [lng, lat], closed. Covers the central bins with a margin.
        public const string ApproximateBoundaryGeoJson =
            """
            {"type":"Polygon","coordinates":[[
            [23.735,37.872],[23.742,37.880],[23.755,37.884],[23.770,37.882],[23.784,37.877],
            [23.790,37.868],[23.787,37.858],[23.778,37.849],[23.765,37.844],[23.750,37.842],
            [23.738,37.847],[23.732,37.856],[23.733,37.865],[23.735,37.872]
            ]]}
            """;

        public static async Task SeedMunicipalityAsync(DataContext context)
        {
            if (await context.Municipalities.AnyAsync()) return;

            context.Municipalities.Add(new Municipality
            {
                Id = MunicipalityService.SingletonId,
                Name = "Glyfada",
                CenterLatitude = 37.8650,
                CenterLongitude = 23.7530,
                DefaultZoom = 14,
                TimeZoneId = "Europe/Athens",
                BoundaryGeoJson = GeoJsonCompact(ApproximateBoundaryGeoJson),
                BoundaryIsApproximate = true,
                BoundarySource = ApproximateSource,
                BoundaryFetchedAt = null,
                StreetlightCount = 1200,
                StreetlightWatts = 60,
                UpdatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        private static string GeoJsonCompact(string json) => string.Concat(json.Where(c => c != '\n' && c != '\r'));
    }
}
