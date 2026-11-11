using API.Data;
using API.DTOs;
using API.Entities;
using API.Errors;
using API.Geo;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    /// <summary>Waste bin management and collection routes.</summary>
    public class WasteService : IWasteService
    {
        public const string OutsideBoundaryMessage = "Bin must be inside the municipality boundary";

        private readonly DataContext _context;
        private readonly IMunicipalityService _municipalities;

        public WasteService(DataContext context, IMunicipalityService municipalities)
        {
            _context = context;
            _municipalities = municipalities;
        }

        /// <summary>Parses an optional type filter. Empty input means "all types".</summary>
        public static bool TryParseType(string value, out WasteType? type)
        {
            type = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!Enum.TryParse<WasteType>(value.Trim(), ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed)) return false;
            type = parsed;
            return true;
        }

        public async Task<List<WasteBinDTO>> GetActiveBinsAsync(WasteType? type)
        {
            var query = _context.WasteBins.Where(b => b.IsActive);
            if (type.HasValue) query = query.Where(b => b.Type == type.Value);

            var bins = await query.OrderBy(b => b.Id).ToListAsync();
            return bins.Select(WasteBinDTO.From).ToList();
        }

        public async Task<WasteBinDTO> CreateBinAsync(WasteBinInputDTO input)
        {
            ValidateFill(input);
            await EnsureInsideBoundaryAsync(input.Latitude, input.Longitude);

            var bin = new WasteBin
            {
                IsActive = true,
                LastEmptied = DateTime.UtcNow,
            };
            Apply(bin, input);

            _context.WasteBins.Add(bin);
            await _context.SaveChangesAsync();
            return WasteBinDTO.From(bin);
        }

        public async Task<WasteBinDTO> UpdateBinAsync(int id, WasteBinInputDTO input)
        {
            var bin = await _context.WasteBins.FindAsync(id);
            if (bin == null) return null;

            ValidateFill(input);
            await EnsureInsideBoundaryAsync(input.Latitude, input.Longitude);

            Apply(bin, input);
            await _context.SaveChangesAsync();
            return WasteBinDTO.From(bin);
        }

        public async Task<bool> DeactivateBinAsync(int id)
        {
            var bin = await _context.WasteBins.FindAsync(id);
            if (bin == null) return false;

            bin.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<WasteBinDTO> MarkEmptiedAsync(int id)
        {
            var bin = await _context.WasteBins.FindAsync(id);
            if (bin == null) return null;

            bin.CurrentFillLevel = 0;
            bin.LastEmptied = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return WasteBinDTO.From(bin);
        }

        public async Task<WasteRouteDTO> BuildRouteAsync(WasteType? type, int minFillPercent)
        {
            var municipality = await _municipalities.GetEntityAsync();
            var depot = new LatLng(municipality.CenterLatitude, municipality.CenterLongitude);

            var query = _context.WasteBins.Where(b => b.IsActive);
            if (type.HasValue) query = query.Where(b => b.Type == type.Value);
            var eligible = (await query.OrderBy(b => b.Id).ToListAsync())
                .Where(b => WasteBinDTO.FillPercentOf(b.Capacity, b.CurrentFillLevel) >= minFillPercent)
                .ToList();

            var points = eligible.Select(b => new LatLng(b.Latitude, b.Longitude)).ToList();
            var order = RouteOptimizer.Optimize(depot, points);
            var totalMeters = RouteOptimizer.TourLengthMeters(depot, points, order);

            var stops = order.Select((index, position) =>
            {
                var bin = eligible[index];
                return new RouteStopDTO
                {
                    BinId = bin.Id,
                    Location = bin.Location,
                    Type = bin.Type.ToString(),
                    Lat = bin.Latitude,
                    Lng = bin.Longitude,
                    FillPercent = WasteBinDTO.FillPercentOf(bin.Capacity, bin.CurrentFillLevel),
                    Order = position + 1,
                };
            }).ToList();

            return new WasteRouteDTO
            {
                Stops = stops,
                TotalDistanceKm = Math.Round(totalMeters / 1000.0, 2),
                Depot = new PointDTO(depot.Lat, depot.Lng),
            };
        }

        private async Task EnsureInsideBoundaryAsync(double lat, double lng)
        {
            if (!await _municipalities.IsInsideBoundaryAsync(lat, lng))
                throw new DomainValidationException(OutsideBoundaryMessage);
        }

        private static void ValidateFill(WasteBinInputDTO input)
        {
            if (input.CurrentFillLevel > input.Capacity)
                throw new DomainValidationException("Fill level cannot exceed capacity");
        }

        private static void Apply(WasteBin bin, WasteBinInputDTO input)
        {
            bin.Location = input.Location.Trim();
            bin.Type = Enum.Parse<WasteType>(input.Type);
            bin.Latitude = input.Latitude;
            bin.Longitude = input.Longitude;
            bin.Capacity = input.Capacity;
            bin.CurrentFillLevel = input.CurrentFillLevel;
        }
    }
}
