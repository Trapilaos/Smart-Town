using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class AuthTests
{
    private readonly HttpClient _client;

    public AuthTests(ApiFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Register_then_login_returns_a_token()
    {
        var username = $"user{Guid.NewGuid():N}"[..12];

        var register = await _client.PostAsJsonAsync("/api/account/register", new { username, password = "Passw0rd" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/account/login", new { username, password = "Passw0rd" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(username, body.GetProperty("username").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_with_taken_username_is_rejected()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var again = await _client.PostAsJsonAsync("/api/account/register", new { username = member.Username, password = "Passw0rd" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_401()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var response = await _client.PostAsJsonAsync("/api/account/login", new { username = member.Username, password = "Wrong123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_failure_message_does_not_reveal_whether_the_user_exists()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);

        var wrongPassword = await _client.PostAsJsonAsync("/api/account/login", new { username = member.Username, password = "Wrong123" });
        var unknownUser = await _client.PostAsJsonAsync("/api/account/login", new { username = "nobody-here", password = "Wrong123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownUser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Repeated_failed_logins_lock_the_account()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);

        for (var i = 0; i < 10; i++)
            await _client.PostAsJsonAsync("/api/account/login", new { username = member.Username, password = "Wrong123" });

        // Even the correct password is refused while locked out.
        var response = await _client.PostAsJsonAsync("/api/account/login", new { username = member.Username, password = "Passw0rd" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
