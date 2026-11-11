using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests
{
    private readonly HttpClient _client;

    public AuthorizationTests(ApiFactory factory) => _client = factory.CreateApiClient();

    private static readonly object Bin = new { location = "Stadiou", type = "General", latitude = 37.8650, longitude = 23.7530, capacity = 100, currentFillLevel = 10 };
    private static readonly object Municipality = new { name = "Glyfada", centerLatitude = 37.8650, centerLongitude = 23.7530, defaultZoom = 14, streetlightCount = 1200, streetlightWatts = 60 };
    private static readonly object BoundaryApply = new { osmType = "R", osmId = 1234 };
    private static readonly object Reservation = new { parkingSpaceId = 1, reservationTime = DateTime.UtcNow.AddMinutes(5), duration = 30 };
    private static readonly object Event = new { title = "Test", date = DateTime.UtcNow.AddDays(3), category = "Culture", isPublished = true };
    private static readonly object Parking = new { name = "Test lot", type = "Lot", latitude = 37.8650, longitude = 23.7530, maxVehicles = 10, hourlyRate = 1.0 };
    private static readonly object Invoice = new { payerName = "Test payer", description = "Test", amount = 10.0, dueDate = DateTime.UtcNow.AddDays(10) };
    private static readonly object PublicRequest = new { invoiceNumber = "INV-2000-000001", paymentCode = "ABCDEFGH" };

    /// <summary>Requests that need a signed-in Member (or any signed-in user). Anonymous calls get 401.</summary>
    public static TheoryData<string, string> AuthenticatedOnly => new()
    {
        { "POST", "/api/parking/reserve" },
        { "GET", "/api/parking/active-reservation" },
        { "GET", "/api/invoices/mine" },
        { "POST", "/api/invoices/INV-2000-000001/pay" },
        { "POST", "/api/events/1/interest" },
        { "DELETE", "/api/events/1/interest" },
    };

    /// <summary>Admin endpoints. Members get 403 and anonymous callers get 401. Ids that do not exist keep the tests free of side effects.</summary>
    public static TheoryData<string, string> AdminOnly => new()
    {
        { "POST", "/api/admin/waste/bins" },
        { "PUT", "/api/admin/waste/bins/9999" },
        { "DELETE", "/api/admin/waste/bins/9999" },
        { "PUT", "/api/admin/waste/bins/9999/empty" },
        { "GET", "/api/admin/waste/route" },
        { "PUT", "/api/admin/municipality" },
        { "POST", "/api/admin/municipality/boundary/apply" },
        { "POST", "/api/traffic/update" },
        { "GET", "/api/comments" },
        { "GET", "/api/admin/parking" },
        { "POST", "/api/admin/parking" },
        { "PUT", "/api/admin/parking/9999" },
        { "DELETE", "/api/admin/parking/9999" },
        { "GET", "/api/admin/parking/9999/reservations" },
        { "GET", "/api/admin/events" },
        { "POST", "/api/admin/events" },
        { "PUT", "/api/admin/events/9999" },
        { "DELETE", "/api/admin/events/9999" },
        { "GET", "/api/admin/invoices" },
        { "POST", "/api/admin/invoices" },
        { "GET", "/api/admin/invoices/summary" },
        { "GET", "/api/admin/audit" },
    };

    private static object BodyFor(string url) => url switch
    {
        "/api/admin/waste/bins" => Bin,
        "/api/admin/waste/bins/9999" => Bin,
        "/api/admin/municipality" => Municipality,
        "/api/admin/municipality/boundary/apply" => BoundaryApply,
        "/api/parking/reserve" => Reservation,
        "/api/admin/parking" => Parking,
        "/api/admin/parking/9999" => Parking,
        "/api/admin/events" => Event,
        "/api/admin/events/9999" => Event,
        "/api/admin/invoices" => Invoice,
        _ => null,
    };

    [Theory]
    [MemberData(nameof(AuthenticatedOnly))]
    [MemberData(nameof(AdminOnly))]
    public async Task Anonymous_requests_get_401(string method, string url)
    {
        var response = await _client.SendAsync(TestHelpers.Request(new HttpMethod(method), url, null, BodyFor(url)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnly))]
    public async Task Member_gets_403_on_admin_endpoints(string method, string url)
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(new HttpMethod(method), url, member, BodyFor(url)));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnly))]
    public async Task Admin_is_allowed_on_admin_endpoints(string method, string url)
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(new HttpMethod(method), url, admin, BodyFor(url)));
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_pay_an_invoice_that_is_not_theirs()
    {
        // The admin issues an invoice without an account, so no member owns it.
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var created = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/invoices", admin,
            new { payerName = "Walk-in", description = "Not for members", amount = 12.5, dueDate = DateTime.UtcNow.AddDays(9) }));
        var invoiceNumber = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("invoiceNumber").GetString();

        var member = await TestHelpers.RegisterMemberAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, $"/api/invoices/{invoiceNumber}/pay", member));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_invoice_endpoints_need_no_login()
    {
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/public/invoices/lookup", null, PublicRequest));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
