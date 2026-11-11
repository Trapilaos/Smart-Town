using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class ParkingAdminTests
{
    private readonly HttpClient _client;

    public ParkingAdminTests(ApiFactory factory) => _client = factory.CreateApiClient();

    private static object Space(string name, double lat, double lng, string type = "Lot", int maxVehicles = 20) => new
    {
        name,
        address = "Test street 1",
        type,
        latitude = lat,
        longitude = lng,
        maxVehicles,
        hourlyRate = 1.5m,
        isActive = true,
    };

    private async Task<JsonElement> CreateSpaceAsync(string name, double lat, double lng, int maxVehicles = 20)
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/parking", admin,
            Space(name, lat, lng, maxVehicles: maxVehicles)));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Admin_cannot_create_a_space_outside_the_boundary()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/parking", admin,
            Space("Far away", 38.0, 23.7530)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Parking space must be inside the municipality boundary", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_parking_type_is_rejected()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/parking", admin,
            Space("Garage", 37.8660, 23.7540, type: "Garage")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_spaces_sit_inside_central_glyfada()
    {
        var spaces = await _client.GetFromJsonAsync<JsonElement>("/api/parking");

        var seeded = spaces.EnumerateArray().Where(s => s.GetProperty("id").GetInt32() <= 6).ToList();
        Assert.Equal(6, seeded.Count);
        foreach (var space in seeded)
        {
            var lat = space.GetProperty("latitude").GetDouble();
            var lng = space.GetProperty("longitude").GetDouble();
            Assert.InRange(lat, 37.855, 37.875);
            Assert.InRange(lng, 23.745, 23.765);
        }
    }

    [Fact]
    public async Task Public_list_shows_free_spaces_now_and_never_who_reserved()
    {
        var space = await CreateSpaceAsync("Lot with one car", 37.8660, 23.7540, maxVehicles: 3);
        var id = space.GetProperty("id").GetInt32();
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var reserve = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/parking/reserve", member,
            new { parkingSpaceId = id, reservationTime = DateTime.UtcNow.AddMinutes(-1), duration = 60 }));
        Assert.Equal(HttpStatusCode.OK, reserve.StatusCode);

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/parking");
        var entry = list.EnumerateArray().Single(s => s.GetProperty("id").GetInt32() == id);

        Assert.Equal(2, entry.GetProperty("freeNow").GetInt32());
        Assert.Equal(1, entry.GetProperty("currentVehicles").GetInt32());
        Assert.DoesNotContain(JsonTestHelpers.AllPropertyNames(list), n => JsonTestHelpers.IdentityFields.Contains(n));
    }

    [Fact]
    public async Task Admin_sees_reservation_holders_by_username_only()
    {
        var space = await CreateSpaceAsync("Lot for reservation list", 37.8670, 23.7550);
        var id = space.GetProperty("id").GetInt32();
        var member = await TestHelpers.RegisterMemberAsync(_client);
        await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/parking/reserve", member,
            new { parkingSpaceId = id, reservationTime = DateTime.UtcNow.AddMinutes(30), duration = 60 }));

        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, $"/api/admin/parking/{id}/reservations", admin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        var row = Assert.Single(rows.EnumerateArray());
        Assert.Equal(member.Username, row.GetProperty("username").GetString());
        Assert.False(row.GetProperty("isActive").GetBoolean(), "a reservation that starts later is upcoming");
        Assert.DoesNotContain(JsonTestHelpers.AllPropertyNames(rows), n => n is "email" or "phoneNumber" or "userId");
    }

    [Fact]
    public async Task Deactivated_space_disappears_from_the_public_list_and_cannot_be_reserved()
    {
        var space = await CreateSpaceAsync("Lot to close", 37.8680, 23.7560);
        var id = space.GetProperty("id").GetInt32();
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var delete = await _client.SendAsync(TestHelpers.Request(HttpMethod.Delete, $"/api/admin/parking/{id}", admin));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/parking");
        Assert.DoesNotContain(list.EnumerateArray(), s => s.GetProperty("id").GetInt32() == id);

        var member = await TestHelpers.RegisterMemberAsync(_client);
        var reserve = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/parking/reserve", member,
            new { parkingSpaceId = id, reservationTime = DateTime.UtcNow.AddMinutes(5), duration = 30 }));
        Assert.Equal(HttpStatusCode.BadRequest, reserve.StatusCode);
    }

    [Fact]
    public async Task Admin_update_moves_a_space_and_refuses_points_outside_the_boundary()
    {
        var space = await CreateSpaceAsync("Lot to move", 37.8690, 23.7570);
        var id = space.GetProperty("id").GetInt32();
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var outside = await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, $"/api/admin/parking/{id}", admin,
            Space("Lot to move", 38.0, 23.7570)));
        Assert.Equal(HttpStatusCode.BadRequest, outside.StatusCode);

        var inside = await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, $"/api/admin/parking/{id}", admin,
            Space("Lot moved", 37.8700, 23.7580, maxVehicles: 9)));
        Assert.Equal(HttpStatusCode.OK, inside.StatusCode);
        var updated = await inside.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Lot moved", updated.GetProperty("name").GetString());
        Assert.Equal(9, updated.GetProperty("maxVehicles").GetInt32());
    }
}
