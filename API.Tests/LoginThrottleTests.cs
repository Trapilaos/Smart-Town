using System.Net;
using System.Net.Http.Json;

namespace API.Tests;

/// <summary>
/// Failed logins are throttled per (username, client address), not per account. The client address comes from
/// X-Forwarded-For here, because the test peer is a trusted proxy (loopback).
/// </summary>
[Collection(ApiCollection.Name)]
public class LoginThrottleTests : IDisposable
{
    private const string Admin = "admin";
    private const string AddressA = "203.0.113.9";
    private const string AddressB = "203.0.113.10";

    private readonly ShortBlockFactory _factory = new();
    private readonly HttpClient _client;

    public LoginThrottleTests() => _client = _factory.CreateApiClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Five_failures_from_one_address_block_that_address_even_with_the_right_password()
    {
        await FailAsync(AddressA, times: 5);

        var correct = await LoginAsync(AddressA, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
    }

    [Fact]
    public async Task The_block_is_per_address_so_the_real_admin_still_logs_in_from_elsewhere()
    {
        await FailAsync(AddressA, times: 5);

        var fromOtherAddress = await LoginAsync(AddressB, ApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, fromOtherAddress.StatusCode);
    }

    [Fact]
    public async Task The_block_lifts_after_the_window()
    {
        await FailAsync(AddressA, times: 5);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(AddressA, ApiFactory.AdminPassword)).StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(ShortBlockFactory.BlockSeconds + 0.5));

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(AddressA, ApiFactory.AdminPassword)).StatusCode);
    }

    [Fact]
    public async Task A_successful_login_clears_the_failure_count_for_that_pair()
    {
        await FailAsync(AddressA, times: 4);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(AddressA, ApiFactory.AdminPassword)).StatusCode);

        // Four more failures would have reached five in total, but the success reset the count.
        await FailAsync(AddressA, times: 4);

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(AddressA, ApiFactory.AdminPassword)).StatusCode);
    }

    [Fact]
    public async Task Usernames_match_exactly_ignoring_case_and_there_is_no_admin_alias()
    {
        var capitalised = await LoginAsync(AddressA, ApiFactory.AdminPassword, username: "Admin");
        Assert.Equal(HttpStatusCode.OK, capitalised.StatusCode);

        var alias = await LoginAsync(AddressA, ApiFactory.AdminPassword, username: "administrator");
        Assert.Equal(HttpStatusCode.Unauthorized, alias.StatusCode);
    }

    private async Task FailAsync(string address, int times)
    {
        for (var i = 0; i < times; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(address, "Wrong123")).StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string address, string password, string username = Admin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/login")
        {
            Content = JsonContent.Create(new { username, password }),
        };
        return _client.SendAsync(ApiFactory.FromRemote(request, "127.0.0.1", address));
    }

    private sealed class ShortBlockFactory : ApiFactory
    {
        public const int BlockSeconds = 2;

        protected override IReadOnlyDictionary<string, string> ExtraSettings => new Dictionary<string, string>
        {
            ["LoginThrottle:BlockSeconds"] = BlockSeconds.ToString(),
        };
    }
}
