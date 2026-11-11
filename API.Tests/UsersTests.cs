using System.Net;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class UsersTests
{
    private readonly HttpClient _client;

    public UsersTests(ApiFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Anonymous_cannot_read_a_user_by_id()
    {
        var response = await _client.GetAsync("/api/users/id/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Member_reading_another_user_gets_403()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        // Seeded users have ids 1..n and the new member has a higher id, so id 1 is someone else.
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/users/id/1", member));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_read_any_user()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/users/id/1", admin));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
