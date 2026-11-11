using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Uses its own host with low limits (public invoice 5 per minute, auth 10 per minute) to prove the 429 responses.
/// The shared factory raises the limits so the other tests never trip them.
/// </summary>
[Collection(ApiCollection.Name)]
public class RateLimitTests : IDisposable
{
    private readonly LowLimitApiFactory _factory = new();
    private readonly HttpClient _client;

    public RateLimitTests() => _client = _factory.CreateApiClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Sixth_public_invoice_lookup_within_a_minute_is_429_with_retry_after()
    {
        var body = new { invoiceNumber = "INV-2000-000001", paymentCode = "ABCDEFGH" };
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage limited = null;

        for (var i = 0; i < 6 && limited == null; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/public/invoices/lookup", body);
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) limited = response;
        }

        Assert.Equal(6, statuses.Count);
        Assert.All(statuses.Take(5), s => Assert.Equal(HttpStatusCode.NotFound, s));
        Assert.NotNull(limited);
        Assert.True(limited.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Public_invoice_pay_shares_the_same_limit_as_lookup()
    {
        var body = new { invoiceNumber = "INV-2000-000002", paymentCode = "ABCDEFGH" };
        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/public/invoices/lookup", body);

        var response = await _client.PostAsJsonAsync("/api/public/invoices/pay", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Eleventh_login_attempt_within_a_minute_is_429()
    {
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/account/login", new { username = "nobody-here", password = "Wrong123" });
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(10), s => Assert.Equal(HttpStatusCode.Unauthorized, s));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    private sealed class LowLimitApiFactory : ApiFactory
    {
        protected override IReadOnlyDictionary<string, string> RateLimitSettings => new Dictionary<string, string>
        {
            ["RateLimits:BoundarySearchPerMinute"] = "10",
            ["RateLimits:AuthPerMinute"] = "10",
            ["RateLimits:PublicInvoicePerMinute"] = "5",
            ["RateLimits:GlobalPerMinute"] = "100000",
        };
    }
}
