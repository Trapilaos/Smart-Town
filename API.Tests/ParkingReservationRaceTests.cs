using API.Data;
using API.Entities;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests;

/// <summary>
/// Concurrent bookings over one SQLite file, each with its own DbContext, as separate requests would have.
/// The check and the insert must be atomic, so only one of ten bookings for the last slot may win.
/// </summary>
public class ParkingReservationRaceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"smarttown-race-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<DataContext> _options;

    public ParkingReservationRaceTests()
    {
        _options = new DbContextOptionsBuilder<DataContext>().UseSqlite($"Data Source={_path}").Options;

        using var context = new DataContext(_options);
        context.Database.EnsureCreated();
        context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Last slot", MaxVehicles = 1 });
        context.SaveChanges();
    }

    [Fact]
    public async Task Ten_concurrent_bookings_for_the_last_slot_produce_exactly_one_success()
    {
        var start = DateTime.UtcNow.AddMinutes(10);

        // A barrier releases all ten at once, each on its own thread, so the bookings really overlap.
        var released = new Barrier(10);
        var attempts = Enumerable.Range(0, 10).Select(i => Task.Factory.StartNew(() =>
        {
            released.SignalAndWait();
            return ReserveFromOwnContextAsync($"user{i}", start).GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(success => success));
        Assert.Equal(9, results.Count(success => !success));

        await using var check = new DataContext(_options);
        Assert.Equal(1, await check.Reservations.CountAsync());
    }

    private async Task<bool> ReserveFromOwnContextAsync(string userId, DateTime start)
    {
        await using var context = new DataContext(_options);
        var service = new ParkingService(context, new MunicipalityService(context), NullLogger<ParkingService>.Instance);

        var (success, _, _) = await service.ReserveParkingSpaceAsync(userId, 1, start, 30);
        return success;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // Temporary file; the OS removes it eventually if it is still locked.
        }
    }
}
