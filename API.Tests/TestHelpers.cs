using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

public record Session(string Username, string Token);

public static class TestHelpers
{
    public static async Task<Session> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/account/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new Session(body.GetProperty("username").GetString(), body.GetProperty("token").GetString());
    }

    public static Task<Session> LoginAdminAsync(HttpClient client) =>
        LoginAsync(client, "admin", ApiFactory.AdminPassword);

    /// <summary>Registers a fresh Member (Identity needs upper, lower and a digit; max 8 chars) and returns its session.</summary>
    public static async Task<Session> RegisterMemberAsync(HttpClient client, string prefix = "mem")
    {
        var username = $"{prefix}{Guid.NewGuid():N}"[..12];
        var response = await client.PostAsJsonAsync("/api/account/register", new { username, password = "Passw0rd" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new Session(body.GetProperty("username").GetString(), body.GetProperty("token").GetString());
    }

    public static HttpRequestMessage Request(HttpMethod method, string url, Session session = null, object body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (session != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        if (body != null)
            request.Content = JsonContent.Create(body);
        return request;
    }
}
