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
        _context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Test", MaxVehicles = 10, CurrentVehicles = 3 });
        await _context.SaveChangesAsync();
        var service = new ParkingService(_context);

        var (success, _, _) = await service.ReserveParkingSpaceAsync("42", 1, DateTime.UtcNow.AddMinutes(1), 30);
        Assert.True(success);
        Assert.Equal(4, (await _context.ParkingSpaces.FindAsync(1)).CurrentVehicles);

        // Make the reservation long expired, then trigger the clean-up that runs on listing.
        var reservation = await _context.Reservations.SingleAsync();
        reservation.ReservationTime = DateTime.UtcNow.AddDays(-2);
        await _context.SaveChangesAsync();

        var spaces = await service.GetParkingSpacesAsync();

        Assert.Equal(3, spaces.Single().CurrentVehicles);
        Assert.Empty(_context.Reservations);
    }

    [Fact]
    public async Task Full_space_rejects_new_reservations()
    {
        _context.ParkingSpaces.Add(new ParkingSpace { Id = 1, Name = "Test", MaxVehicles = 1, CurrentVehicles = 1 });
        await _context.SaveChangesAsync();

        var (success, _, _) = await new ParkingService(_context).ReserveParkingSpaceAsync("42", 1, DateTime.UtcNow.AddMinutes(1), 30);

        Assert.False(success);
    }
}
