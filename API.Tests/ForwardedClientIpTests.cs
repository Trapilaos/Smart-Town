using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Proves that the rate limiter keys on the real client from X-Forwarded-For, but only when the TCP peer is a
/// trusted proxy. Uses a host with a public-invoice limit of 5 per minute, like RateLimitTests.
/// </summary>
[Collection(ApiCollection.Name)]
public class ForwardedClientIpTests : IDisposable
{
    private const string Trusted = "127.0.0.1";     // loopback is trusted by default
    private const string Untrusted = "198.51.100.7"; // any other peer is not

    private readonly LowLimitFactory _factory = new();
    private readonly HttpClient _client;

    public ForwardedClientIpTests() => _client = _factory.CreateApiClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Two_forwarded_clients_behind_a_trusted_proxy_get_separate_buckets()
    {
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await SendLookupAsync(Trusted, "203.0.113.1")).StatusCode);

        // The first client is now over its budget...
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendLookupAsync(Trusted, "203.0.113.1")).StatusCode);

        // ...but the second client still has its own full budget.
        Assert.Equal(HttpStatusCode.NotFound, (await SendLookupAsync(Trusted, "203.0.113.2")).StatusCode);
    }

    [Fact]
    public async Task Forwarded_header_from_an_untrusted_peer_is_ignored()
    {
        // Both values arrive from the same untrusted peer, so they share one bucket: the sixth request is limited.
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await SendLookupAsync(Untrusted, $"203.0.113.{i + 1}")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendLookupAsync(Untrusted, "203.0.113.99")).StatusCode);
    }

    [Fact]
    public async Task A_client_cannot_choose_its_bucket_by_prepending_addresses()
    {
        // The proxy appends the real address last. With ForwardLimit = 1 only that last entry is used.
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await SendLookupAsync(Trusted, $"10.0.0.{i + 1}, 203.0.113.1")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendLookupAsync(Trusted, "192.0.2.50, 203.0.113.1")).StatusCode);
    }

    private Task<HttpResponseMessage> SendLookupAsync(string remoteIp, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/invoices/lookup")
        {
            Content = JsonContent.Create(new { invoiceNumber = "INV-2000-000001", paymentCode = "ABCDEFGH" }),
        };
        return _client.SendAsync(ApiFactory.FromRemote(request, remoteIp, forwardedFor));
    }

    private sealed class LowLimitFactory : ApiFactory
    {
        protected override IReadOnlyDictionary<string, string> RateLimitSettings => new Dictionary<string, string>
        {
            ["RateLimits:BoundarySearchPerMinute"] = "10",
            ["RateLimits:AuthPerMinute"] = "100000",
            ["RateLimits:PublicInvoicePerMinute"] = "5",
            ["RateLimits:GlobalPerMinute"] = "100000",
        };
    }
}
