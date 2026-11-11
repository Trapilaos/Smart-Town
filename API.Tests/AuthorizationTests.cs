using System.Net;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests
{
    private readonly HttpClient _client;

    public AuthorizationTests(ApiFactory factory) => _client = factory.CreateApiClient();

    private static readonly object Bin = new { location = "Stadiou", type = "General", latitude = 37.8650, longitude = 23.7530, capacity = 100, currentFillLevel = 10 };
    private static readonly object Municipality = new { name = "Glyfada", centerLatitude = 37.8650, centerLongitude = 23.7530, defaultZoom = 14, streetlightCount = 1200, streetlightWatts = 60 };
    private static readonly object BoundaryApply = new { osmType = "R", osmId = 1234 };
    private static readonly object BoundarySearch = new { query = "Glyfada, Attica, Greece" };
    private static readonly object Reservation = new { parkingSpaceId = 1, reservationTime = DateTime.UtcNow.AddMinutes(5), duration = 30 };
    private static readonly object Payment = new { invoiceNumber = "INV1-01", amount = 10, currency = "EUR", description = "x" };

    public static TheoryData<string, string> AuthenticatedOnly => new()
    {
        { "POST", "/api/payment" },
        { "POST", "/api/parking/reserve" },
        { "GET", "/api/parking/active-reservation" },
    };

    public static TheoryData<string, string> AdminOnly => new()
    {
        { "POST", "/api/admin/waste/bins" },
        { "PUT", "/api/admin/waste/bins/1" },
        { "DELETE", "/api/admin/waste/bins/1" },
        { "PUT", "/api/admin/waste/bins/1/empty" },
        { "GET", "/api/admin/waste/route" },
        { "PUT", "/api/admin/municipality" },
        { "POST", "/api/admin/municipality/boundary/apply" },
        { "POST", "/api/traffic/update" },
        { "GET", "/api/comments" },
    };

    private static object BodyFor(string url) => url switch
    {
        "/api/admin/waste/bins" => Bin,
        "/api/admin/waste/bins/1" => Bin,
        "/api/admin/municipality" => Municipality,
        "/api/admin/municipality/boundary/apply" => BoundaryApply,
        "/api/parking/reserve" => Reservation,
        "/api/payment" => Payment,
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
    public async Task Member_cannot_pay_someone_elses_invoice()
    {
        // Seeded invoices INV1-xx belong to user "1"; a freshly registered member has a different id.
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/payment", member, Payment));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
