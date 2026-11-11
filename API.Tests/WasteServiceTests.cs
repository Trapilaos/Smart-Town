using API.Data;
using API.Data.Seeds;
using API.DTOs;
using API.Entities;
using API.Errors;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

/// <summary>Bin management rules: the boundary check, fill validation, soft delete and emptying.</summary>
public class WasteServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DataContext _context;
    private readonly WasteService _waste;

    public WasteServiceTests()
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

    private static WasteBinInputDTO Input(double lat, double lng, int fill = 10) => new()
    {
        Location = "Test street 1",
        Type = "Paper",
        Latitude = lat,
        Longitude = lng,
        Capacity = 240,
        CurrentFillLevel = fill,
    };

    [Fact]
    public async Task Creating_a_bin_outside_the_boundary_is_rejected()
    {
        var error = await Assert.ThrowsAsync<DomainValidationException>(() => _waste.CreateBinAsync(Input(38.0, 23.7530)));

        Assert.Equal("Bin must be inside the municipality boundary", error.Message);
    }

    [Fact]
    public async Task Creating_a_bin_inside_the_boundary_succeeds()
    {
        var bin = await _waste.CreateBinAsync(Input(37.8650, 23.7530));

        Assert.True(bin.IsActive);
        Assert.Equal("Paper", bin.Type);
        Assert.Equal(4.2, bin.FillPercent); // 10 of 240 litres
    }

    [Fact]
    public async Task Moving_a_bin_outside_the_boundary_is_rejected()
    {
        var existing = await _context.WasteBins.OrderBy(b => b.Id).FirstAsync();

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _waste.UpdateBinAsync(existing.Id, Input(37.80, 23.90)));
    }

    [Fact]
    public async Task Fill_above_capacity_is_rejected()
    {
        var input = Input(37.8650, 23.7530, fill: 300);

        await Assert.ThrowsAsync<DomainValidationException>(() => _waste.CreateBinAsync(input));
    }

    [Fact]
    public async Task Updating_an_unknown_bin_returns_null()
    {
        Assert.Null(await _waste.UpdateBinAsync(99999, Input(37.8650, 23.7530)));
    }

    [Fact]
    public async Task Delete_is_a_soft_delete_that_hides_the_bin_from_the_public_list()
    {
        var target = await _context.WasteBins.OrderBy(b => b.Id).FirstAsync();

        Assert.True(await _waste.DeactivateBinAsync(target.Id));

        var stored = await _context.WasteBins.FindAsync(target.Id);
        Assert.NotNull(stored);
        Assert.False(stored.IsActive);
        Assert.DoesNotContain((await _waste.GetActiveBinsAsync(null)), b => b.Id == target.Id);
    }

    [Fact]
    public async Task Emptying_a_bin_sets_fill_to_zero_and_updates_last_emptied()
    {
        var target = await _context.WasteBins.OrderBy(b => b.Id).FirstAsync();
        var before = DateTime.UtcNow;

        var emptied = await _waste.MarkEmptiedAsync(target.Id);

        Assert.Equal(0, emptied.CurrentFillLevel);
        Assert.Equal(0, emptied.FillPercent);
        Assert.True(emptied.LastEmptied >= before.AddSeconds(-1));
    }

    [Fact]
    public async Task Public_list_can_be_filtered_by_type()
    {
        var glass = await _waste.GetActiveBinsAsync(WasteType.Glass);

        Assert.NotEmpty(glass);
        Assert.All(glass, b => Assert.Equal("Glass", b.Type));
    }

    [Theory]
    [InlineData("General", true, WasteType.General)]
    [InlineData("recycling", true, WasteType.Recycling)]
    [InlineData("", true, null)]
    [InlineData("Plastic", false, null)]
    public void Type_filter_parsing(string value, bool ok, WasteType? expected)
    {
        var parsed = WasteService.TryParseType(value, out var type);

        Assert.Equal(ok, parsed);
        if (ok) Assert.Equal(expected, type);
    }
}
