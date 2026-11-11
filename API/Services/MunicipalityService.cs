using System.Text.Json;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Geo;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class MunicipalityService : IMunicipalityService
    {
        public const int SingletonId = 1;
        public const string ManualSource = "Manual upload";

        private readonly DataContext _context;

        public MunicipalityService(DataContext context)
        {
            _context = context;
        }

        public async Task<Municipality> GetEntityAsync()
        {
            return await _context.Municipalities.FirstOrDefaultAsync(m => m.Id == SingletonId)
                ?? throw new InvalidOperationException("The municipality has not been seeded.");
        }

        public async Task<MunicipalityDTO> GetAsync()
        {
            return ToDto(await GetEntityAsync());
        }

        public async Task<MunicipalityDTO> UpdateAsync(MunicipalityUpdateDTO dto)
        {
            // Parse the boundary before touching the entity so a bad boundary leaves the row unchanged.
            GeoGeometry boundary = null;
            if (dto.Boundary.HasValue)
            {
                boundary = GeoService.ParseGeoJson(dto.Boundary.Value.GetRawText());
            }

            var municipality = await GetEntityAsync();
            municipality.Name = dto.Name.Trim();
            municipality.CenterLatitude = dto.CenterLatitude;
            municipality.CenterLongitude = dto.CenterLongitude;
            municipality.DefaultZoom = dto.DefaultZoom;
            municipality.StreetlightCount = dto.StreetlightCount;
            municipality.StreetlightWatts = dto.StreetlightWatts;

            if (boundary != null)
            {
                municipality.BoundaryGeoJson = GeoService.ToGeoJson(boundary);
                municipality.BoundaryIsApproximate = false;
                municipality.BoundarySource = ManualSource;
                municipality.BoundaryFetchedAt = null;
            }

            municipality.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToDto(municipality);
        }

        public async Task<GeoGeometry> GetBoundaryAsync()
        {
            var municipality = await GetEntityAsync();
            return GeoService.ParseGeoJson(municipality.BoundaryGeoJson);
        }

        public async Task<bool> IsInsideBoundaryAsync(double lat, double lng)
        {
            return GeoService.IsInside(lat, lng, await GetBoundaryAsync());
        }

        public async Task ApplyBoundaryAsync(BoundaryCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            var municipality = await GetEntityAsync();
            municipality.BoundaryGeoJson = GeoService.ToGeoJson(candidate.Geometry);
            municipality.BoundaryIsApproximate = false;
            municipality.BoundarySource = $"OpenStreetMap {OsmTypeName(candidate.OsmType)} {candidate.OsmId}";
            municipality.BoundaryFetchedAt = DateTime.UtcNow;
            municipality.CenterLatitude = candidate.CenterLat;
            municipality.CenterLongitude = candidate.CenterLng;
            municipality.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public static string OsmTypeName(string osmType) => osmType.ToUpperInvariant() switch
        {
            "R" => "relation",
            "W" => "way",
            "N" => "node",
            _ => throw new ArgumentException("Unknown OSM type", nameof(osmType)),
        };

        public static MunicipalityDTO ToDto(Municipality m)
        {
            using var document = JsonDocument.Parse(m.BoundaryGeoJson);
            return new MunicipalityDTO
            {
                Name = m.Name,
                CenterLatitude = m.CenterLatitude,
                CenterLongitude = m.CenterLongitude,
                DefaultZoom = m.DefaultZoom,
                TimeZoneId = m.TimeZoneId,
                Boundary = document.RootElement.Clone(),
                BoundaryIsApproximate = m.BoundaryIsApproximate,
                BoundarySource = m.BoundarySource,
                BoundaryFetchedAt = m.BoundaryFetchedAt,
                StreetlightCount = m.StreetlightCount,
                StreetlightWatts = m.StreetlightWatts,
                UpdatedAt = m.UpdatedAt,
            };
        }
    }
}
