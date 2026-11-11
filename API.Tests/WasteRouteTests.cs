using API.Data;
using API.Entities;
using API.Extensions;
using API.Services;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

public class WasteRouteTests : IDisposable
{
    private static readonly string[] Locations = ["3is Septemvriou", "Alexandras", "Stadiou", "Vouliagmenis"];

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public WasteRouteTests()
    {
        _connection.Open();
        _context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _mapper = new MapperConfiguration(c => c.AddMaps(typeof(API.Helpers.AutoMapperProfiles).Assembly)).CreateMapper();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task AddBinsAsync(Func<string, int> fill)
    {
        var id = 1;
        foreach (var location in Locations)
            _context.WasteBins.Add(new WasteBin { Id = id++, Location = location, Capacity = 100, CurrentFillLevel = fill(location), LastEmptied = DateTime.UtcNow });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Optimal_path_is_a_closed_route_over_every_non_empty_bin()
    {
        await AddBinsAsync(_ => 50);
        var service = new WasteManagementService(_context, _mapper);

        var (needsPath, path) = await service.GetOptimalPathAsync();

        Assert.True(needsPath);
        Assert.True(path.Count >= 3);
        Assert.Equal(path.First(), path.Last());                 // returns to the depot
        Assert.Equal(Locations.Length, path.Distinct().Count()); // visits every bin
        Assert.All(path, location => Assert.Contains(location, Locations));
    }

    [Fact]
    public async Task Route_starts_at_the_fullest_bin()
    {
        await AddBinsAsync(l => l == "Stadiou" ? 95 : 10);
        var service = new WasteManagementService(_context, _mapper);

        var (_, path) = await service.GetOptimalPathAsync();

        Assert.Equal("Stadiou", path.First());
    }

    [Fact]
    public async Task No_route_is_needed_when_every_bin_is_empty()
    {
        await AddBinsAsync(_ => 0);
        var service = new WasteManagementService(_context, _mapper);

        var (needsPath, _) = await service.GetOptimalPathAsync();

        Assert.False(needsPath);
    }
}
