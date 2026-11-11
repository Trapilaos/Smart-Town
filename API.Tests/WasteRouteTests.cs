using API.Data;
using API.Data.Seeds;
using API.Entities;
using API.Errors;
using API.Geo;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

/// <summary>Collection routes computed from the database: the seeded bins, filters and the depot.</summary>
public class WasteRouteTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DataContext _context;
    private readonly WasteService _waste;

    public WasteRouteTests()
    {
        _connection.Open();
        _context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        MunicipalitySeed.SeedMunicipalityAsync(_context).GetAwaiter().GetResult();
        WasteBinSeed.SeedWasteBinsAsync(_context).GetAwaiter().GetResult();
        _waste = new WasteService(_context, new MunicipalityService(_context));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Route_visits_every_eligible_bin_once()
    {
        var active = await _context.WasteBins.CountAsync(b => b.IsActive);

        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 0);

        Assert.Equal(active, route.Stops.Count);
        Assert.Equal(route.Stops.Count, route.Stops.Select(s => s.BinId).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, route.Stops.Count), route.Stops.Select(s => s.Order));
    }

    [Fact]
    public async Task Route_starts_and_ends_at_the_municipality_centre()
    {
        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 0);
        var municipality = await _context.Municipalities.SingleAsync();

        Assert.Equal(municipality.CenterLatitude, route.Depot.Lat);
        Assert.Equal(municipality.CenterLongitude, route.Depot.Lng);
        Assert.True(route.TotalDistanceKm > 0);
    }

    [Fact]
    public async Task Route_total_distance_is_the_closed_tour_length()
    {
        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 0);
        var points = route.Stops.Select(s => new LatLng(s.Lat, s.Lng)).ToList();
        var depot = new LatLng(route.Depot.Lat, route.Depot.Lng);
        var order = Enumerable.Range(0, points.Count).ToList();

        var metres = RouteOptimizer.TourLengthMeters(depot, points, order);

        Assert.Equal(Math.Round(metres / 1000.0, 2), route.TotalDistanceKm, precision: 2);
    }

    [Fact]
    public async Task Route_only_contains_bins_at_or_above_the_fill_threshold()
    {
        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 60);

        Assert.NotEmpty(route.Stops);
        Assert.All(route.Stops, stop => Assert.True(stop.FillPercent >= 60, $"bin {stop.BinId} at {stop.FillPercent}%"));
    }

    [Fact]
    public async Task Route_respects_the_type_filter()
    {
        var route = await _waste.BuildRouteAsync(WasteType.Glass, minFillPercent: 0);

        Assert.NotEmpty(route.Stops);
        Assert.All(route.Stops, stop => Assert.Equal("Glass", stop.Type));
    }

    [Fact]
    public async Task Inactive_bins_are_not_collected()
    {
        var target = await _context.WasteBins.Where(b => b.IsActive).OrderBy(b => b.Id).FirstAsync();
        await _waste.DeactivateBinAsync(target.Id);

        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 0);

        Assert.DoesNotContain(route.Stops, s => s.BinId == target.Id);
    }

    [Fact]
    public async Task Route_over_no_eligible_bins_is_empty_with_zero_distance()
    {
        var route = await _waste.BuildRouteAsync(type: null, minFillPercent: 101);

        Assert.Empty(route.Stops);
        Assert.Equal(0, route.TotalDistanceKm);
    }
}
