using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class SecurityAndAuditTests
{
    private readonly HttpClient _client;

    public SecurityAndAuditTests(ApiFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        var response = await _client.GetAsync("/api/municipality");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("geolocation=(), camera=(), microphone=()", response.Headers.GetValues("Permissions-Policy").Single());
        // The test host is not Development, so HSTS is sent.
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Error_responses_carry_the_security_headers_too()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task Utc_dates_are_serialised_with_a_z_suffix()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/municipality");

        Assert.EndsWith("Z", body.GetProperty("updatedAt").GetString());
    }

    [Fact]
    public async Task Lighting_offsets_are_still_local_not_utc()
    {
        var rows = await _client.GetFromJsonAsync<JsonElement>("/api/lighting/schedule?days=1");

        Assert.Matches(@"[+-]\d{2}:\d{2}$", rows[0].GetProperty("sunrise").GetString());
    }

    [Theory]
    [InlineData("GET", "/api/payment/check/INV1-01")]
    [InlineData("POST", "/api/payment")]
    [InlineData("POST", "/api/events")]
    [InlineData("POST", "/api/admin/create-event")]
    public async Task Removed_endpoints_no_longer_accept_requests(string method, string url)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"{method} {url} returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Admin_bin_actions_are_written_to_the_audit_log()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var bin = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/waste/bins", admin,
            new { location = "Audit street 1", type = "Paper", latitude = 37.8660, longitude = 23.7540, capacity = 240, currentFillLevel = 0 }));
        Assert.Equal(HttpStatusCode.OK, bin.StatusCode);
        var id = (await bin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, $"/api/admin/waste/bins/{id}/empty", admin));

        var entries = await AuditAsync(admin, 50);
        Assert.Contains(entries, e => e.Action == "waste.bin.create" && e.EntityId == id.ToString() && e.Username == "admin");
        Assert.Contains(entries, e => e.Action == "waste.bin.empty" && e.EntityId == id.ToString());
    }

    [Fact]
    public async Task Municipality_update_is_audited()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var body = new { name = "Glyfada", centerLatitude = 37.8650, centerLongitude = 23.7530, defaultZoom = 14, streetlightCount = 1200, streetlightWatts = 60 };

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Put, "/api/admin/municipality", admin, body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains(await AuditAsync(admin, 50), e => e.Action == "municipality.update");
    }

    [Fact]
    public async Task Parking_event_and_invoice_admin_actions_are_audited_without_payment_codes()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/parking", admin,
            new { name = "Audited lot", type = "Lot", latitude = 37.8660, longitude = 23.7540, maxVehicles = 5, hourlyRate = 1.0m, isActive = true }));
        await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/events", admin,
            new { title = "Audited event", date = DateTime.UtcNow.AddDays(4), category = "Other", isPublished = true }));
        var invoice = await (await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/invoices", admin,
            new { payerName = "Audited payer", description = "Audited bill", amount = 9.99m, dueDate = DateTime.UtcNow.AddDays(3) })))
            .Content.ReadFromJsonAsync<JsonElement>();
        var code = invoice.GetProperty("paymentCode").GetString();

        var entries = await AuditAsync(admin, 50);

        Assert.Contains(entries, e => e.Action == "parking.create");
        Assert.Contains(entries, e => e.Action == "event.create");
        Assert.Contains(entries, e => e.Action == "invoice.create");
        Assert.DoesNotContain(entries, e => e.Details != null && e.Details.Contains(code));
    }

    [Fact]
    public async Task Audit_take_is_limited_to_1_to_200()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/audit?take=0", admin));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<List<AuditRow>> AuditAsync(Session admin, int take)
    {
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, $"/api/admin/audit?take={take}", admin));
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>();
        return rows.EnumerateArray().Select(r => new AuditRow(
            r.GetProperty("action").GetString(),
            r.GetProperty("entityId").GetString(),
            r.GetProperty("username").GetString(),
            r.TryGetProperty("details", out var d) ? d.GetString() : null)).ToList();
    }

    private sealed record AuditRow(string Action, string EntityId, string Username, string Details);
}
