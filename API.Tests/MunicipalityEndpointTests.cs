using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class MunicipalityEndpointTests
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public MunicipalityEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Municipality_is_public_and_returns_the_seeded_settings()
    {
        var response = await _client.GetAsync("/api/municipality");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Glyfada", body.GetProperty("name").GetString());
        Assert.Equal("Europe/Athens", body.GetProperty("timeZoneId").GetString());
        Assert.Equal("Polygon", body.GetProperty("boundary").GetProperty("type").GetString());
        Assert.True(body.GetProperty("boundaryIsApproximate").GetBoolean());
        Assert.False(body.TryGetProperty("id", out _), "the public payload must not expose internal ids");
    }

    [Fact]
    public async Task Admin_municipality_update_rejects_a_bad_boundary_with_400()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var body = new
        {
            name = "Glyfada",
            centerLatitude = 37.8650,
            centerLongitude = 23.7530,
            defaultZoom = 14,
            streetlightCount = 1200,
            streetlightWatts = 60,
            boundary = new { type = "Polygon", coordinates = new[] { new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 } } } },
        };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, "/api/admin/municipality", admin, body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_municipality_update_rejects_out_of_range_centre_with_400()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var body = new { name = "Glyfada", centerLatitude = 95, centerLongitude = 23.7530, defaultZoom = 14, streetlightCount = 1200, streetlightWatts = 60 };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, "/api/admin/municipality", admin, body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_bin_outside_the_boundary_returns_400_with_the_plan_message()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var body = new { location = "Far away", type = "General", latitude = 38.0, longitude = 23.7530, capacity = 240, currentFillLevel = 0 };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/waste/bins", admin, body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Plain text here because the client sent no Accept header; with Accept: application/json it is a JSON string.
        Assert.Contains("Bin must be inside the municipality boundary", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Creating_a_bin_inside_the_boundary_returns_the_bin_and_it_is_listed_publicly()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var body = new { location = "Kanari 3", type = "Glass", latitude = 37.8660, longitude = 23.7540, capacity = 240, currentFillLevel = 120 };

        var created = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/waste/bins", admin, body));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var bin = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Glass", bin.GetProperty("type").GetString());
        Assert.Equal(50, bin.GetProperty("fillPercent").GetDouble());

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/waste/bins?type=Glass");
        Assert.Contains(list.EnumerateArray(), b => b.GetProperty("id").GetInt32() == bin.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Public_bins_list_filters_by_type_and_rejects_unknown_types()
    {
        var ok = await _client.GetAsync("/api/waste/bins?type=Paper");
        var unknown = await _client.GetAsync("/api/waste/bins?type=Plastic");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.All((await ok.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
            b => Assert.Equal("Paper", b.GetProperty("type").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task Admin_route_returns_stops_with_depot_and_distance()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/waste/route?minFillPercent=0", admin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = body.GetProperty("stops");
        Assert.True(stops.GetArrayLength() > 0);
        var first = stops[0];
        Assert.True(first.TryGetProperty("binId", out _));
        Assert.True(first.TryGetProperty("order", out _));
        Assert.True(first.TryGetProperty("lat", out _));
        Assert.True(first.TryGetProperty("lng", out _));
        Assert.True(body.GetProperty("totalDistanceKm").GetDouble() > 0);
        Assert.True(body.GetProperty("depot").TryGetProperty("lat", out _));
    }

    [Theory]
    [InlineData("/api/waste")]
    [InlineData("/api/waste/optimalpath")]
    [InlineData("/api/lighting/status?town=Glyfada")]
    public async Task Removed_endpoints_return_404(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lighting_schedule_returns_the_requested_number_of_days()
    {
        var response = await _client.GetAsync("/api/lighting/schedule?days=7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, rows.GetArrayLength());
        var row = rows[0];
        Assert.True(row.TryGetProperty("lightsOn", out _));
        Assert.True(row.TryGetProperty("dimStart", out _));
        Assert.True(row.TryGetProperty("dimEnd", out _));
        Assert.True(row.TryGetProperty("lightsOff", out _));
        Assert.True(row.TryGetProperty("litHours", out _));
        Assert.True(row.TryGetProperty("estimatedKwh", out _));
        Assert.Matches(@"[+-]\d{2}:\d{2}$", row.GetProperty("sunrise").GetString());
    }

    [Theory]
    [InlineData("/api/lighting/schedule?days=0")]
    [InlineData("/api/lighting/schedule?days=32")]
    public async Task Lighting_schedule_rejects_days_outside_1_to_31(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lighting_now_reports_status_brightness_and_next_change()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/lighting/now");

        var status = body.GetProperty("status").GetString();
        Assert.Contains(status, new[] { "On", "Dimmed", "Off" });
        Assert.True(body.TryGetProperty("brightnessPercent", out _));
        Assert.True(body.GetProperty("nextChange").TryGetProperty("at", out _));
        Assert.Contains(body.GetProperty("nextChange").GetProperty("toStatus").GetString(), new[] { "On", "Dimmed", "Off" });
    }

    [Fact]
    public async Task Admin_boundary_apply_rejects_an_object_that_is_not_a_boundary_with_400()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/municipality/boundary/apply", admin,
            new { osmType = "N", osmId = 77 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Boundary_search_is_rate_limited_to_10_per_minute_with_retry_after()
    {
        // This is the only test that calls the search endpoint, so its window is not shared with other tests.
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage limited = null;

        for (var i = 0; i < 12 && limited == null; i++)
        {
            var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/municipality/boundary/search", admin,
                new { query = "Glyfada, Attica, Greece" }));
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) limited = response;
        }

        Assert.NotNull(limited);
        Assert.Equal(11, statuses.Count);
        Assert.All(statuses.Take(10), s => Assert.Equal(HttpStatusCode.OK, s));
        Assert.True(limited.Headers.Contains("Retry-After"));
        Assert.True(_factory.NominatimStub.Requests.Count >= 10);
    }
}
