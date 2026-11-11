using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using API.Data;
using API.Entities;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class ParkingApiTests
{
    private readonly HttpClient _client;

    public ParkingApiTests(ApiFactory factory) => _client = factory.CreateApiClient();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1441)]
    public async Task Reserve_with_out_of_range_duration_is_400(int duration)
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var body = new { parkingSpaceId = 1, reservationTime = DateTime.UtcNow.AddMinutes(5), duration };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/parking/reserve", member, body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_with_valid_duration_succeeds()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var body = new { parkingSpaceId = 1, reservationTime = DateTime.UtcNow.AddMinutes(5), duration = 30 };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/parking/reserve", member, body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

/// <summary>Service-level tests on a private in-memory SQLite database.</summary>
public class ParkingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DataContext _context;

    public ParkingServiceTests()
    {
        _connection.Open();
        _context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Expired_reservation_frees_exactly_one_slot()
    {
        // Occupancy is derived from active reservations, so a seeded CurrentVehicles value is overwritten.
        _context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Test", MaxVehicles = 10, CurrentVehicles = 0 });
        await _context.SaveChangesAsync();
        var service = new ParkingService(_context, NullLogger<ParkingService>.Instance);

        var (success, _, _) = await service.ReserveParkingSpaceAsync("42", 1, DateTime.UtcNow.AddMinutes(1), 30);
        Assert.True(success);
        Assert.Equal(1, (await _context.ParkingSpaces.FindAsync(1)).CurrentVehicles);

        // Make the reservation long expired, then trigger the clean-up that runs on listing.
        var reservation = await _context.Reservations.SingleAsync();
        reservation.ReservationTime = DateTime.UtcNow.AddDays(-2);
        await _context.SaveChangesAsync();

        var spaces = await service.GetParkingSpacesAsync();

        Assert.Equal(0, spaces.Single().CurrentVehicles);
        Assert.Empty(_context.Reservations);
    }

    private ParkingService NewService() => new(_context, NullLogger<ParkingService>.Instance);

    private async Task SeedSpaceWithReservationAsync(int maxVehicles, DateTime otherUserStart, int otherUserMinutes)
    {
        _context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Test", MaxVehicles = maxVehicles });
        _context.Reservations.Add(new Reservation { UserId = "7", ParkingSpaceId = 1, ReservationTime = otherUserStart, Duration = otherUserMinutes });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Full_space_rejects_overlapping_reservation()
    {
        await SeedSpaceWithReservationAsync(1, DateTime.UtcNow, 60);

        var (success, _, _) = await NewService().ReserveParkingSpaceAsync("42", 1, DateTime.UtcNow.AddMinutes(1), 30);

        Assert.False(success);
    }

    [Fact]
    public async Task Back_to_back_bookings_count_as_one_car_at_a_time()
    {
        // Three consecutive one-hour bookings overlap a new three-hour window, but never each other,
        // so at most one car is parked at any moment and a 2-slot space still has room.
        var start = DateTime.UtcNow.AddHours(1);
        _context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Test", MaxVehicles = 2 });
        for (var i = 0; i < 3; i++)
            _context.Reservations.Add(new Reservation { UserId = $"other{i}", ParkingSpaceId = 1, ReservationTime = start.AddHours(i), Duration = 60 });
        await _context.SaveChangesAsync();

        var (success, _, _) = await NewService().ReserveParkingSpaceAsync("42", 1, start, 180);

        Assert.True(success);
    }

    [Fact]
    public async Task Future_reservation_does_not_block_a_non_overlapping_one()
    {
        // Someone has the only slot tomorrow; parking now for 30 minutes should still be allowed.
        await SeedSpaceWithReservationAsync(1, DateTime.UtcNow.AddDays(1), 60);

        var (success, _, _) = await NewService().ReserveParkingSpaceAsync("42", 1, DateTime.UtcNow.AddMinutes(1), 30);

        Assert.True(success);
    }
}
